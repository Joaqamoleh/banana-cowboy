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
        EASE_IN_OUT_BOUNCE
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
        EaseInBounce, EaseOutBounce, EaseInOutBounce
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
        return t;
    }

    public static float EaseOutCubic(float t)
    {
        return t;
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
        return t;
    }

    public static float EaseOutCirc(float t)
    {
        return t;
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
        return t;
    }

    public static float EaseInOutBack(float t)
    {
        return t;
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
}
