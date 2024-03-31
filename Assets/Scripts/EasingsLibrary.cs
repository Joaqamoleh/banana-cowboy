using UnityEditor.Rendering;
using static UnityEngine.Mathf;
public class EasingsLibrary
{
    public delegate float Function(float t);

    public enum FunctionName
    {
        EASE_IN_SIN,
        EASE_OUT_SIN, 
        EASE_IN_OUT_SIN,
        EASE_IN_QUAD,
        EASE_OUT_QUAD,
        EASE_IN_OUT_QUAD,
        EASE_IN_CUBIC,
        EASE_OUT_CUBIC,
        EASE_IN_OUT_CUBIC,
        EASE_IN_QUART,
        EASE_OUT_QUART,
        EASE_IN_OUT_QUART,
        EASE_IN_QUINT,
        EASE_OUT_QUINT,
        EASE_IN_OUT_QUINT,
        EASE_IN_EXPO,
        EASE_OUT_EXPO,
        EASE_IN_OUT_EXPO,
        EASE_IN_CIRC,
        EASE_OUT_CIRC,
        EASE_IN_OUT_CIRC,
        EASE_IN_BACK,
        EASE_OUT_BACK,
        EASE_IN_OUT_BACK,
        EASE_IN_ELASTIC,
        EASE_OUT_ELASTIC,
        EASE_IN_OUT_ELASTIC,
        EASE_IN_BOUNCE,
        EASE_OUT_BOUNCE,
        EASE_IN_OUT_BOUNCE,
        NO_OP,
    }

    static Function[] functions = { 
        EaseInSin, EaseOutSin, EaseInOutSin, 
        EaseInQuad, EaseOutQuad, EaseInOutQuad,
        EaseInCubic, EaseOutCubic, EaseInOutCubic,
        EaseInQuart, EaseOutQuart, EaseInOutQuart,
        EaseInQuint, EaseOutQuint, EaseInOutQuint,
        EaseInExpo, EaseOutExpo, EaseInOutExpo,
        EaseInCirc, EaseOutCirc, EaseInOutCirc,
        EaseInBack, EaseOutBack, EaseInOutBack,
        EaseInElastic, EaseOutElastic, EaseInOutElastic,
        EaseInBounce, EaseOutBounce, EaseInOutBounce,
        NoOp
    };
    public static int FunctionCount => functions.Length;

    public static Function GetFunction(FunctionName name) => functions[(int)name];

    public static float EaseInSin(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }

        return 1 - Cos((t * PI) / 2);
    }

    public static float EaseOutSin(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }

        return Sin((t * PI) / 2);
    }

    public static float EaseInOutSin(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }

        return -(Cos(PI * t) - 1) / 2;
    }

    public static float EaseInQuad(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }

        return t * t;
    }

    public static float EaseOutQuad(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }

        return 1f - (1f - t) * (1f - t);
    }

    public static float EaseInOutQuad(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }

        return t < 0.5 ? 2 * t * t : 1 - Pow(-2 * t + 2, 2) / 2;
    }

    public static float EaseInCubic(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }

        return t * t * t;
    }

    public static float EaseOutCubic(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }

        return 1 - Pow(1 - t, 3);
    }

    public static float EaseInOutCubic(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }

        return t < 0.5 ? 4 * t * t * t : 1 - Pow(-2 * t + 2, 3) / 2;
    }

    public static float EaseInQuart(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }

        return t * t * t * t;
    }

    public static float EaseOutQuart(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }

        return 1f - Pow(1f - t, 4f);
    }

    public static float EaseInOutQuart(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }

        return t < 0.5 ? 8 * t * t * t * t : 1 - Pow(-2 * t + 2, 4) / 2;
    }

    public static float EaseInQuint(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }

        return t * t * t * t * t;
    }

    public static float EaseOutQuint(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }
        return 1f - Pow(1 - t, 5);
    }

    public static float EaseInOutQuint(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }

        return t < 0.5f ? 16 * t * t * t * t * t : 1f - Pow(-2f * t + 2f, 5f) / 2f;
    }

    public static float EaseInExpo(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }

        return Pow(2, 10f * t - 10f);
    }

    public static float EaseOutExpo(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }

        return 1f - Pow(2f, -10f * t);
    }

    public static float EaseInOutExpo(float t)
    {
        if (t >= 1) { return 1f; }
        if (t <= 0) { return 0f; }

        return t < 0.5f ? Pow(2, 20 * t - 10) / 2 : (2 - Pow(2, -20 * t + 10)) / 2;
    }

    public static float EaseInCirc(float t)
    {
        if (t >= 1) { return 1; }
        if (t <= 0) { return 0; }

        return 1 - Sqrt(1 - Pow(t, 2));
    }

    public static float EaseOutCirc(float t)
    {
        if (t >= 1) { return 1; }
        if (t <= 0) { return 0; }

        return Sqrt(1 - Pow(t - 1, 2));
    }

    public static float EaseInOutCirc(float t)
    {
        if (t >= 1) { return 1; }
        if (t <= 0) { return 0; }

        return t < 0.5f ? (1f - Sqrt(1f - Pow(2f * t, 2f))) / 2 : (Sqrt(1 - Pow(-2 * t + 2, 2f)) + 1) / 2;
    }

    public static float EaseInBack(float t)
    {
        if (t >= 1) { return 1; }
        if (t <= 0) { return 0; }

        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return c3 * t * t * t - c1 * t * t;
    }

    public static float EaseOutBack(float t)
    {
        if (t >= 1f) { return 1f; }
        if (t <= 0f) { return 0f; }

        float c1 = 1.70158f;
        float c3 = c1 + 1;

        return 1 + c3 * Pow(t - 1, 3) + c1 * Pow(t - 1, 2);
    }

    public static float EaseInOutBack(float t)
    {
        if (t >= 1f) { return 1f; }
        if (t <= 0f) { return 0f; }

        float c1 = 1.70158f;
        float c2 = c1 * 1.525f;

        return t < 0.5
          ? (Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2
          : (Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
    }

    public static float EaseInElastic(float t)
    {
        if (t >= 1) { return 1; }
        if (t <= 0) { return 0; }

        float c4 = (2f * PI) / 3f;

        return -Pow(2f, 10 * t - 10) * Sin((t * 10 - 10.75f) * c4);
    }

    public static float EaseOutElastic(float t)
    {
        if (t >= 1) { return 1; }
        if (t <= 0) { return 0; }

        float c4 = (2f * PI) / 3f;

        return Pow(2, -10 * t) * Sin((t * 10f - 0.75f) * c4) + 1;
    }

    public static float EaseInOutElastic(float t)
    {
        if (t >= 1) { return 1; }
        if (t <= 0) { return 0; }

        float c5 = (2f * PI) / 4.5f;

        return t < 0.5f 
            ? -(Pow(2, 20 * t - 10) * Sin((20 * t - 11.125f) * c5)) / 2f
            : (Pow(2, -20 * t + 10) * Sin((20 * t - 11.125f) * c5)) / 2f + 1f;
    }

    public static float EaseInBounce(float t)
    {
        return 1f - EaseOutBounce(t);
    }

    public static float EaseOutBounce(float t)
    {
        if (t >= 1) { return 1; }
        if (t <= 0) { return 0; }

        float n1 = 7.5625f;
        float d1 = 2.75f;

        if (t < 1 / d1)
        {
            return n1 * t * t;
        }
        else if (t < 2 / d1)
        {
            return n1 * (t -= 1.5f / d1) * t + 0.75f;
        }
        else if (t < 2.5 / d1)
        {
            return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        }
        else
        {
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }
    }

    public static float EaseInOutBounce(float t)
    {
        return t < 0.5
          ? (1 - EaseOutBounce(1 - 2 * t)) / 2
          : (1 + EaseOutBounce(2 * t - 1)) / 2;
    }

    public static float NoOp(float t)
    {
        return t;
    }
}
