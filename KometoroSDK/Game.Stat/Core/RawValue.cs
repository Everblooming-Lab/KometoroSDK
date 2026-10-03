using System;

namespace EverbloomingLab.KometoroSDK.Game
{
    public readonly struct RawValue : IEquatable<RawValue>, IComparable<RawValue>
    {
        /// <summary> 全局容差，用于 <see cref="IsZero"/>、<see cref="ApproxEquals"/> 等容差判断。 </summary>
        public const double EPSILON = 1e-7;

        public static readonly RawValue _Zero = new RawValue(0.0);
        public static readonly RawValue _One = new RawValue(1.0);
        public static readonly RawValue _MinusOne = new RawValue(-1.0);

        /// <summary> 底层 double 值。 </summary>
        public double Value { get; }

        /// <summary> 构造。 </summary>
        public RawValue(double d) => Value = d;

        /// <summary> 严格等于 0（无容差，仅用于特殊场景）。 </summary>
        public bool IsStrictZero => Value == 0;

        /// <summary> 在容差范围内视为零。 </summary>
        public bool IsZero => Math.Abs(Value) < EPSILON;

        /// <summary> 在容差范围外视为非零。 </summary>
        public bool IsNonZero => Math.Abs(Value) >= EPSILON;

        /// <summary> 大于容差，视为正数。 </summary>
        public bool IsPositive => Value > EPSILON;

        /// <summary> 小于负容差，视为负数。 </summary>
        public bool IsNegative => Value < -EPSILON;

        /// <summary> 符号：-1 / 0 / +1（基于 <see cref="Math.Sign(double)"/>，无容差）。 </summary>
        public int Sign => Math.Sign(Value);

        /// <summary> 是否为 NaN。 </summary>
        public bool IsNaN => double.IsNaN(Value);

        /// <summary> 是否为正/负无穷。 </summary>
        public bool IsInfinity => double.IsInfinity(Value);

        /// <summary> 是否为有限数（非 NaN 且非无穷）。 </summary>
        public bool IsFinite => !double.IsNaN(Value) && !double.IsInfinity(Value);

        /// <summary> 绝对值。 </summary>
        public RawValue Abs => new RawValue(Math.Abs(Value));

        /// <summary> 四舍五入到整数。 </summary>
        public RawValue Rounded => new RawValue(Math.Round(Value));

        /// <summary> 向上取整。 </summary>
        public RawValue Ceiling => new RawValue(Math.Ceiling(Value));

        /// <summary> 向下取整。 </summary>
        public RawValue Floored => new RawValue(Math.Floor(Value));

        /// <summary> 如果为负则归零。 </summary>
        public RawValue ClampToZero => new RawValue(Math.Max(0.0, Value));

        /// <summary> 限制下限，返回不小于 <paramref name="min"/> 的值。 </summary>
        public RawValue AtLeast(double min) => new RawValue(Math.Max(min, Value));

        /// <summary> 限制上限，返回不大于 <paramref name="max"/> 的值。 </summary>
        public RawValue AtMost(double max) => new RawValue(Math.Min(max, Value));

        /// <summary> 将值钳制在 [<paramref name="min"/>, <paramref name="max"/>] 范围内。 </summary>
        public RawValue Clamp(double min, double max) =>
            new RawValue(Math.Max(min, Math.Min(max, Value)));

        /// <summary> 四舍五入为 int。 </summary>
        public int AsInt => (int)Math.Round(Value);

        /// <summary> 向上取整为 int。 </summary>
        public int AsIntCeiling => (int)Math.Ceiling(Value);

        /// <summary> 向下取整为 int。 </summary>
        public int AsIntFloor => (int)Math.Floor(Value);

        /// <summary> 四舍五入为 long。 </summary>
        public long AsLong => (long)Math.Round(Value);

        /// <summary> 向上取整为 long。 </summary>
        public long AsLongCeiling => (long)Math.Ceiling(Value);

        /// <summary> 向下取整为 long。 </summary>
        public long AsLongFloor => (long)Math.Floor(Value);

        /// <summary> 转换为 float（可能丢失精度）。 </summary>
        public float AsFloat => (float)Value;

        /// <summary> RawValue → double（隐式）。 </summary>
        public static implicit operator double(RawValue r) => r.Value;

        /// <summary> double → RawValue（显式，需写 <c>(RawValue)1.5</c>）。 </summary>
        public static explicit operator RawValue(double d) => new RawValue(d);

        /// <summary> 精确相等（无容差）。容差比较请使用 <see cref="ApproxEquals"/>。 </summary>
        public bool Equals(RawValue other) => Value.Equals(other.Value);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is RawValue other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(RawValue left, RawValue right) => left.Equals(right);
        public static bool operator !=(RawValue left, RawValue right) => !left.Equals(right);

        public static RawValue operator +(RawValue a, RawValue b) => new RawValue(a.Value + b.Value);
        public static RawValue operator -(RawValue a, RawValue b) => new RawValue(a.Value - b.Value);
        public static RawValue operator *(RawValue a, RawValue b) => new RawValue(a.Value * b.Value);
        public static RawValue operator /(RawValue a, RawValue b) => new RawValue(a.Value / b.Value);
        public static RawValue operator %(RawValue a, RawValue b) => new RawValue(a.Value % b.Value);

        public static RawValue operator -(RawValue a) => new RawValue(-a.Value);
        public static RawValue operator +(RawValue a) => a;
        public static RawValue operator ++(RawValue a) => new RawValue(a.Value + 1);
        public static RawValue operator --(RawValue a) => new RawValue(a.Value - 1);

        public static bool operator <(RawValue left, RawValue right) => left.Value  < right.Value;
        public static bool operator >(RawValue left, RawValue right) => left.Value  > right.Value;
        public static bool operator <=(RawValue left, RawValue right) => left.Value <= right.Value;
        public static bool operator >=(RawValue left, RawValue right) => left.Value >= right.Value;

        /// <summary> 容差相等：|this - other| &lt; epsilon。 </summary>
        public bool ApproxEquals(RawValue other, double epsilon = EPSILON)
            => Math.Abs(Value - other.Value) < epsilon;

        /// <summary> 容差小于：this &lt; other - epsilon。 </summary>
        public bool ApproxLessThan(RawValue other, double epsilon = EPSILON)
            => Value < other.Value - epsilon;

        /// <summary> 容差大于：this &gt; other + epsilon。 </summary>
        public bool ApproxGreaterThan(RawValue other, double epsilon = EPSILON)
            => Value > other.Value + epsilon;

        /// <summary> 返回两者中较小的值。 </summary>
        public static RawValue Min(RawValue a, RawValue b) => new RawValue(Math.Min(a.Value, b.Value));

        /// <summary> 返回两者中较大的值。 </summary>
        public static RawValue Max(RawValue a, RawValue b) => new RawValue(Math.Max(a.Value, b.Value));

        /// <summary> 线性插值：a + (b - a) × t。 </summary>
        public static RawValue Lerp(RawValue a, RawValue b, double t)
            => new RawValue(a.Value + (b.Value - a.Value) * t);

        /// <summary> 比较大小，可直接用于 <c>List&lt;RawValue&gt;.Sort()</c>。 </summary>
        public int CompareTo(RawValue other) => Value.CompareTo(other.Value);

        /// <inheritdoc/>
        public override string ToString() => Value.ToString();

        /// <summary> 按指定格式字符串输出（如 <c>"F2"</c>、<c>"P1"</c>）。 </summary>
        public string ToString(string format) => Value.ToString(format);
    }
}