# Week 1 evidence

Keep this short. Bullet points, a small table and one diagram or code trace are enough. This is your checkpoint record, not an extra report. Replace the prompts as you work.

Name: Anton Sellbom

Exact Unity version: 6000.3.26f1

Checkpoint commit / repository: https://github.com/ses5/Welcome-To-AI

Current stage and anything unfinished: Stage 1

## Initial prediction

Three observations the rival needs, including one it might not have:
- Where scrap pickups are and whether they are visible to it right now (range and line of sight).
- Whether it is already carrying scrap, and where its depot is, so it knows when to deliver instead of collecting.
- Whether a known pickup is still available and actually reachable. It might not have this: if the scrap is hidden behind an obstacle, someone else may already have taken it, and a route can turn out to be blocked or impossible only after it starts moving.

Seeing a pickup across a gap does not prove reachability because: Simply being able to see a pickup does not mean the rival can reach it. There may be obstacles, gaps, or other environmental factors that prevent access.

One question about NPC behaviour: How do bosses in the Soulsborne games choose between multiple attack options and attack chains?

## One complete trace

By moving a wall next to the rival's spawn point and rebaking the NavMesh, I blocked the rival's path to the nearest scrap pickup. The sensor (`PickupSensor.Capture`) reported the pickup as available and nearest. The starter policy in `DecisionPolicy.Infer` deliberately ignores the Visible, Remembered and Blocked flags and simply selects the nearest available candidate, so it chose the pickup behind the wall whether the rival could see it or not. This gave `HasUsableTarget = true`, which only means that a target id was selected, not that it can be reached. `DecisionPolicy.Choose` then returned Collect with my reason "Nearest available pickup; sight, memory and blocked ignored", which is shown on the HUD's Reason line (`RaceHud`) via `RivalBrain.CurrentDecision`.

`RivalBrain` sent the target to `Navigator.Request`, which called `SetDestination`, and the status became Pending. Because the wall cut off the NavMesh, the agent only got a partial path, and `Navigator.Poll` to `NavigationFeedback.Classify` returned Failed. Seeing a pickup therefore does not prove that it is reachable. Visibility comes from the sensor's line of sight, while reachability comes from the NavMesh.

On the next decision `RivalBrain` saw the failed route and marked the pickup as blocked for six seconds. Since the policy ignores Blocked, it chose the same pickup again and the rival got stuck. This is the weakness to fix in Stage 4.

## Scenario record

| Scenario | Predicted result | Observed result and short evidence / remaining issue |
|---|---|---|
| Normal loop and restart | | |
| Carrying cargo | | |
| Hidden / remembered pickup | | |
| Target taken | | |
| Unreachable target | | |
| No usable pickup | | |

Supplied test result, with any added tests identified:
Live walkthrough checked by / recording link if used:

## One correction and one independent check

The original weakness and why it happened:
What I changed and why it addresses the cause:
My counterexample or experiment: prediction, observed result, and one limitation:

## Collaboration and help

People I discussed or playtested with:
Code, sources or substantive tools/AI assistance I used, and how I verified them:
If unfinished: what works, exact reproduction steps, and the help I need:
