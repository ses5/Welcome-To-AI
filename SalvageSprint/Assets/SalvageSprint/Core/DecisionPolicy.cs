namespace SalvageSprint.Core
{
    // Delivery is supplied so the unmodified game has a complete scoring loop.
    public static class DecisionPolicy
    {
        public const string Label = "Anton v4: ignore blocked targets";

        // Stage 2 truth table (before distance is considered):
        //   available + visible             -> known, usable
        //   available + hidden + unknown    -> NOT known (sensor candidate only)
        //   available + hidden + remembered -> known, usable (memory persists)
        //   unavailable + remembered        -> known but NOT usable
        private static bool IsEligible(Candidate candidate)
        {
            if (candidate.Blocked)
                return false;

            return candidate.Available && (candidate.Visible || candidate.Remembered);
        }

        public static AgentFacts Infer(Observation observation)
        {
            if (observation.Carrying)
                return new AgentFacts(true, -1, false);

            // Retain the current target if it is still eligible, even if there is a closer one.
            // This stops the rival from updating closest candidate and oscillating between two pickups
            foreach (Candidate candidate in observation.Candidates)
            {
                if (candidate.Id == observation.CurrentTargetId && IsEligible(candidate))
                    return new AgentFacts(false, candidate.Id, true);
            }

            int nearest = -1;
            float distance = float.PositiveInfinity;
            foreach (Candidate candidate in observation.Candidates)
            {
                // Filter first, then rank by distance.
                if (IsEligible(candidate) && candidate.DistanceSquared < distance)
                { nearest = candidate.Id; distance = candidate.DistanceSquared; }
            }

            return new AgentFacts(false, nearest, false);
        }

        public static Decision Choose(Observation observation) => Choose(Infer(observation));

        public static Decision Choose(AgentFacts facts)
        {
            if (facts.NeedsDelivery)
                return new Decision(TaskKind.Deliver, -1, "Cargo held: return to depot");

            if (!facts.HasUsableTarget)
                return new Decision(TaskKind.Explore, -1, "No eligible known pickup: visit the next survey point");

            string reason = facts.RetainedTarget ? "Retain current eligible target" : "Nearest eligible known pickup";

            return new Decision(TaskKind.Collect, facts.SelectedTargetId, reason);
        }
    }
}
