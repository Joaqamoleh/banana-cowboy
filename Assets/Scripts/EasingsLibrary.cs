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
        return t;
    }

    public static float EaseOutSin(float t)
    {
        return t;
    }

    public static float EaseInOutSin(float t)
    {
        return t;
    }

    public static float EaseInQuad(float t)
    {
        return t;
    }

    public static float EaseOutQuad(float t)
    {
        return t;
    }

    public static float EaseInOutQuad(float t)
    {
        return t;
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
        return t;
    }

    public static float EaseInQuart(float t)
    {
        return t;
    }

    public static float EaseOutQuart(float t)
    {
        return t;
    }

    public static float EaseInOutQuart(float t)
    {
        return t;
    }

    public static float EaseInQuint(float t)
    {
        return t;
    }

    public static float EaseOutQuint(float t)
    {
        return t;
    }

    public static float EaseInOutQuint(float t)
    {
        return t;
    }

    public static float EaseInExpo(float t)
    {
        return t;
    }

    public static float EaseOutExpo(float t)
    {
        return t;
    }

    public static float EaseInOutExpo(float t)
    {
        return t;
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
        return t;
    }

    public static float EaseInBack(float t)
    {
        return t;
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
        return t;
    }

    public static float EaseOutElastic(float t)
    {
        return t;
    }

    public static float EaseInOutElastic(float t)
    {
        return t;
    }

    public static float EaseInBounce(float t)
    {
        return t;
    }

    public static float EaseOutBounce(float t)
    {
        return t;
    }

    public static float EaseInOutBounce(float t)
    {
        return t;
    }

    public static float NoOp(float t)
    {
        return t;
    }
}
