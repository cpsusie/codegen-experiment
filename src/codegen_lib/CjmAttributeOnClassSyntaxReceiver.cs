using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
using Duration = HpTimeStamps.Duration;
namespace Cjm.CodeGen
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using MonoStampSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;
    public abstract class CjmAttributeOnClassSyntaxReceiver<TTargetData> : ISyntaxReceiver where TTargetData : struct, ITargetData, IEquatable<TTargetData>
    {
        public bool IsFrozen => _frozen.IsSet;

        public bool HasTargetData => _frozen.IsSet && !_targetData.IsDefaultOrEmpty;

        public ImmutableArray<TTargetData> TargetData
        {
            get
            {
                return (_frozen.IsSet, _targetData.IsDefaultOrEmpty) switch
                {
                    (false, _) => ImmutableArray<TTargetData>.Empty,
                    (true, true) => ImmutableArray<TTargetData>.Empty,
                    (true, false) => _targetData
                };
            }
        }
        

        public void OnVisitSyntaxNode(SyntaxNode visited)
        {
            TTargetData? result = ExtractTargetDataFromNodeOrNot(visited);
            if (result.HasValue)
            {
                SetTargetData(result.Value);
            }
        }

        public bool FreezeAndQueryHasTargetData(Duration? timeout = null, CancellationToken token = default)
        {
            Freeze(timeout, token);
            return HasTargetData;
        }

        public void Freeze(Duration? timeout=null, CancellationToken token=default)
        {
            if (_frozen.IsSet) return;
            var startedAt = MonoStampSource.StampNow;
            MonotonicStamp quitAt = timeout.HasValue && timeout > Duration.Zero
                ? startedAt + timeout.Value
                : MonotonicStamp.MaxValue;
            while (!_frozen.IsSet && MonoStampSource.StampNow < quitAt)
            {
                if (_frozen.TryBeginSetting())
                {
                    ImmutableArray<TTargetData> contents;
                    try
                    {
                        Debug.Assert(_arrayBldr != null);
                        contents = _arrayBldr!.ToImmutable();
                    }
                    catch (Exception ex)
                    {
                        TraceLog.LogException(ex);
                        _frozen.AbortSettingOrThrow();
                        throw;
                    }

                    _targetData = contents;
                    Debug.Assert(!_targetData.IsDefault);
                    try
                    {
                        _frozen.CompleteSettingOrThrow();
                        _arrayBldr = null;
                    }
                    catch (Exception ex)
                    {
                        TraceLog.LogException(ex);
                        if (_frozen.IsSetting)
                        {
                            _frozen.AbortSettingOrThrow();
                        }
                        throw;
                    }
                }
                else
                {
                    token.ThrowIfCancellationRequested();
                }
            }
            Debug.Assert(_frozen.IsSet == !_targetData.IsDefault);
            if (!_frozen.IsSet)
            {
                throw new TimeoutException("Unable to freeze the flag within maximum specified period" +
                                           (timeout.HasValue
                                               ? (" of " + timeout.Value.TotalMilliseconds.ToString("N4") +
                                                  " milliseconds")
                                               : "."));
            }

        }

        protected abstract TTargetData? ExtractTargetDataFromNodeOrNot(SyntaxNode visited);

        private void SetTargetData(TTargetData setMe)
        {
            if (_frozen.TryBeginUpdating())
            {
                Debug.Assert(_arrayBldr != null);
                try
                {
                    _arrayBldr!.Add(setMe);
                }
                finally
                {
                    _frozen.CompleteUpdatingOrThrow();
                }
            }
            else
            {
                TraceLog.LogError(
                    $"Unable to add target data {setMe.ToString()} to collection because collection is or is being frozen.");
            }
        }

        protected static bool IsPublicStaticPartialClassDeclaration(ClassDeclarationSyntax cds)
        {
            bool foundPublic = false;
            bool foundStatic = false;
            bool foundPartial = false;
            foreach (var modifier in cds.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.PublicKeyword))
                {
                    foundPublic = true;
                }

                if (modifier.IsKind(SyntaxKind.StaticKeyword))
                {
                    foundStatic = true;
                }

                if (modifier.IsKind(SyntaxKind.PartialKeyword))
                {
                    foundPartial = true;
                }

                if (foundStatic && foundPublic && foundPartial)
                    break;
            }

            return foundStatic && foundPublic && foundPartial;
        }

        protected static AttributeSyntax? FindExtensionsAttribute(ClassDeclarationSyntax cds, string attributeShortName)
        {
            foreach (var attribList in cds.AttributeLists)
            {
                foreach (var attrib in attribList.Attributes)
                {

                    if (attrib.Name.ToString() == attributeShortName)
                        return attrib;
                }
            }
            return null;
        }

        private ImmutableArray<TTargetData> _targetData;
        private ImmutableArray<TTargetData>.Builder? _arrayBldr = ImmutableArray.CreateBuilder<TTargetData>();
        private LocklessFourStepFlag _frozen;
    }

    [DebuggerDisplay("{nameof(LocklessFourStepFlag)}--{Status}")]
    struct LocklessFourStepFlag
    {
        public string Status
        {
            get
            {
                int val = _value;
                return val switch
                {
                    Set => "SET",
                    Clear => "CLEAR",
                    Updating => "UPDATING",
                    Setting => "SETTING",
                    _ => "UNKNOWN-ERROR",
                };
            }
        }

        public bool IsSet
        {
            get
            {
                int val = _value;
                return val == Set;
            }
        }

        public bool IsSetting
        {
            get
            {
                int val = _value;
                return val == Setting;
            }
        }

        public bool IsClear
        {
            get
            {
                int val = _value;
                return val == Clear;
            }
        }

        public bool IsUpdating
        {
            get
            {
                int val = _value;
                return val == Updating;
            }
        }

        public bool TryBeginSetting()
        {
            const int wantToBe = Setting;
            const int needToBeNow = Clear;
            return Interlocked.CompareExchange(ref _value, wantToBe, needToBeNow) == needToBeNow;
        }

        public bool TryBeginUpdating()
        {
            const int wantToBe = Updating;
            const int needToBeNow = Clear;
            return Interlocked.CompareExchange(ref _value, wantToBe, needToBeNow) == needToBeNow;
        }

        public void CompleteUpdatingOrThrow()
        {
            if (!TryResetUpdating()) throw new InvalidOperationException("The flag is not in an updating state.");
        }

        public void CompleteSettingOrThrow()
        {
            if (!TryCompleteSetting())
                throw new InvalidOperationException("Flag was not in a setting state.");
        }

        public void AbortSettingOrThrow()
        {
            if (!TryAbortSetting())
                throw new InvalidOperationException("Flag was not in a setting state.");
        }

        private bool TryResetUpdating()
        {
            const int wantToBe = Clear;
            const int needToBeNow = Updating;
            return Interlocked.CompareExchange(ref _value, wantToBe, needToBeNow) == needToBeNow;
        }

        private bool TryAbortSetting()
        {
            const int wantToBe = Clear;
            const int needToBeNow = Setting;
            return Interlocked.CompareExchange(ref _value, wantToBe, needToBeNow) == needToBeNow;
        }

        private bool TryCompleteSetting()
        {
            const int wantToBe = Set;
            const int needToBeNow = Setting;
            return Interlocked.CompareExchange(ref _value, wantToBe, needToBeNow) == needToBeNow;
        }


        private volatile int _value;
        private const int Clear = default(int);
        private const int Setting = 1;
        private const int Set = 2;
        private const int Updating = 3;
    }
}