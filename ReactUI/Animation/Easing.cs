namespace ReactUI.Animation;

/// <summary>
/// Standard CSS easing functions implemented via cubic bezier curves.
/// All functions map t in [0,1] to an eased value in [0,1].
/// </summary>
public static class Easing
{
    public static float Evaluate(Style.EasingType type, float t) => type switch
    {
        Style.EasingType.Linear => t,
        Style.EasingType.Ease => CubicBezier(t, 0.25f, 0.1f, 0.25f, 1.0f),
        Style.EasingType.EaseIn => CubicBezier(t, 0.42f, 0f, 1f, 1f),
        Style.EasingType.EaseOut => CubicBezier(t, 0f, 0f, 0.58f, 1f),
        Style.EasingType.EaseInOut => CubicBezier(t, 0.42f, 0f, 0.58f, 1f),
        _ => t,
    };

    /// <summary>
    /// Evaluates a cubic bezier easing curve defined by control points (x1,y1) and (x2,y2).
    /// Uses Newton-Raphson iteration to solve for the parametric t that yields the input x,
    /// then evaluates the y at that parametric t.
    /// </summary>
    public static float CubicBezier(float t, float x1, float y1, float x2, float y2)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;

        // The cubic bezier curve is defined parametrically:
        //   B(s) = 3(1-s)^2*s*P1 + 3(1-s)*s^2*P2 + s^3
        // We need to find s such that Bx(s) = t, then return By(s).

        // Newton-Raphson: solve Bx(s) - t = 0
        float s = t; // initial guess
        for (int i = 0; i < 8; i++)
        {
            float bx = SampleCurve(s, x1, x2) - t;
            float dx = SampleCurveDerivative(s, x1, x2);
            if (System.MathF.Abs(dx) < 1e-6f) break;
            s -= bx / dx;
        }

        // Clamp s to [0,1]
        s = System.Math.Clamp(s, 0f, 1f);

        // If Newton didn't converge well, fall back to bisection
        float check = SampleCurve(s, x1, x2);
        if (System.MathF.Abs(check - t) > 1e-4f)
        {
            s = BisectCurve(t, x1, x2);
        }

        return SampleCurve(s, y1, y2);
    }

    /// <summary>
    /// Evaluate the cubic bezier polynomial: 3(1-s)^2*s*p1 + 3(1-s)*s^2*p2 + s^3
    /// </summary>
    static float SampleCurve(float s, float p1, float p2)
    {
        // Expanded form: ((1-3*p2+3*p1)*s + (3*p2-6*p1))*s + 3*p1)*s
        float a = 1f - 3f * p2 + 3f * p1;
        float b = 3f * p2 - 6f * p1;
        float c = 3f * p1;
        return ((a * s + b) * s + c) * s;
    }

    /// <summary>
    /// Derivative of the cubic bezier polynomial.
    /// </summary>
    static float SampleCurveDerivative(float s, float p1, float p2)
    {
        float a = 1f - 3f * p2 + 3f * p1;
        float b = 3f * p2 - 6f * p1;
        float c = 3f * p1;
        return (3f * a * s + 2f * b) * s + c;
    }

    /// <summary>
    /// Bisection fallback for when Newton-Raphson does not converge.
    /// </summary>
    static float BisectCurve(float t, float p1, float p2)
    {
        float lo = 0f, hi = 1f;
        float s = t;
        for (int i = 0; i < 20; i++)
        {
            s = (lo + hi) * 0.5f;
            float val = SampleCurve(s, p1, p2);
            if (val > t)
                hi = s;
            else
                lo = s;
        }
        return s;
    }
}
