public static class TrBdf2
{
    public const double Gamma = 0.5857864376269050;
    public const double C1 = 1.2071067811865475;
    public const double C0 = -0.2071067811865476;

    // length of the step of one stage
    public static double StepCoeff(double dt) => Gamma * dt * 0.5;

    // where the stage will end
    public static double StageTime(int stage) => stage == 0 ? Gamma : 1.0;

    // i = C dv/dt  ->  i = G*(v1 - v2) + I0
    public static (double G, double I0) Capacitive(int stage, double c, Component s, double dt)
    {
        double g = c / StepCoeff(dt);
        double i0 =
            stage == 0
                ? -(g * s.V + (s.I)) // TR
                : -g * (C1 * s.Vg + C0 * s.V); // BDF2
        return (g, i0);
    }

    // v = L di/dt
    public static (double G, double I0) Inductive(int stage, double l, Component s, double dt)
    {
        double g = StepCoeff(dt) / l;
        double i0 =
            stage == 0
                ? (s.I) + g * s.V // TR
                : C1 * s.Ig + C0 * (s.I); // BDF2
        return (g, i0);
    }
}
