# Week 1 evidence

Keep this short. Bullet points, a small table and one diagram or code trace are enough. This is your checkpoint record, not an extra report. Replace the prompts as you work.

Name: Anton Sellbom

Exact Unity version: 6000.3.26f1

Checkpoint commit / repository: https://github.com/ses5/Welcome-To-AI

Current stage and anything unfinished: Stage 5 complete (22/22 successful tests, 5 additional tests were added as per the teachers request). Part 1 finished.

## Initial prediction

Three observations the rival needs, including one it might not have:
- Where scrap pickups are and whether they are visible to it right now (range and line of sight).
- Whether it is already carrying scrap, and where its depot is, so it knows when to deliver instead of collecting.
- Whether a known pickup is still available and actually reachable. It might not have this: if the scrap is hidden behind an obstacle, someone else may already have taken it, and a route can turn out to be blocked or impossible only after it starts moving.

Seeing a pickup across a gap does not prove reachability because: Simply being able to see a pickup does not mean the rival can reach it. There may be obstacles, gaps, or other environmental factors that prevent access.

One question about NPC behaviour: How do bosses in the Soulsborne games choose between multiple attack options and attack chains?

## One complete trace

By moving a wall next to the rival's spawn point and rebaking the NavMesh, I blocked the rival's path to the nearest scrap pickup. The sensor (`PickupSensor.Capture`) reported the pickup as available and nearest. The starter policy in `DecisionPolicy.Infer` deliberately ignores the `Visible`, `Remembered` and `Blocked` flags and simply selects the nearest available candidate, so it chose the pickup behind the wall whether the rival could see it or not. This gave `HasUsableTarget = true`, which only means that a target id was selected, not that it can be reached. `DecisionPolicy.Choose` then returned `Collect` with my reason "Nearest available pickup; sight, memory and blocked ignored", which is shown on the HUD's Reason line (`RaceHud`) via `RivalBrain.CurrentDecision`.

`RivalBrain` sent the target to `Navigator.Request`, which called `SetDestination`, and the status became `Pending`. Because the wall cut off the NavMesh, the agent only got a partial path, and `Navigator.Poll` to `NavigationFeedback.Classify` returned `Failed`. Seeing a pickup therefore does not prove that it is reachable. Visibility comes from the sensor's line of sight, while reachability comes from the NavMesh.

On the next decision `RivalBrain` saw the failed route and marked the pickup as blocked for six seconds. Since the policy ignores `Blocked`, it chose the same pickup again and the rival got stuck. This is the weakness to fix in Stage 4.

## Task transitions

Explore (discovers a known pickup) --> Collect

Collect (target stays usable) --> Collect (retained)

Collect (target unavailable / no longer known) --> Collect (other known target) or Explore

Collect (successful collection: Carrying = true) --> Deliver

Deliver (delivery banks scrap, Carrying = false) --> Collect (if a known target exists) or Explore

Explore (arrived at / failed survey point )--> Explore (next survey point)

## Scenario record

| Scenario | Predicted result | Observed result and short evidence / remaining issue |
|---|---|---|
| Normal loop and restart | The rival collects a known pickup, banks it, continues with the next one, the match ends with a result and R restarts it cleanly | F2: The rival goes to the closest eligible Target, returns it to the depot, and repeats this until the match is over. If no Target is eligible, the rival will explore. Pressing R restarted the match with scores reset and the rival collecting again. |
| Carrying cargo | The rival delivers its cargo first and ignores other scrap until it has been delivered | F3 start: Cargo True, Task Deliver, reason "Cargo held: return to depot". The rival went straight to its depot and ignored the visible scrap on the way. After banking the cargo it switched to Collect and pursued the nearest eligible known pickup. Matches the prediction: cargo priority comes before collection in `DecisionPolicy.Infer`. |
| Hidden / remembered pickup | The rival ignores the hidden pickup and goes to a visible one, or explores | F4 start: two candidates in range; Scrap 0 (closest) is hidden behind the occlusion wall, Scrap 1 (further away) is visible. Rival chose Target 1, reason "Nearest eligible known pickup" (HUD: Visible 1). The starter would have chosen the nearer hidden Scrap 0. |
| Target taken | The rival changes its target to the closest visible one | F5 start: only Scrap 0 is in sensor range (HUD: Visible 1); rival chose Target 0, "Nearest eligible known pickup". When the helper took Scrap 0, the rival released it: Target -1, "No eligible known pickup: visit the next survey point". My prediction was wrong, no other pickup was known, so it explored instead of switching. While exploring it discovered Scrap 1, collected and delivered it, then later Scrap 2. With no pickups left it keeps patrolling survey points without crashing or inventing targets. |
| Unreachable target | The rival ignores the unreachable scrap and goes to a visible one, or explores | F6 start: Task: Collect, Scrap 0 is visible, Target 0 is chosen. Scrap 1 is visible (HUD: Visible 2). Target switches to Target 1, reason "Nearest eligible known pickup". This switch happens because Target 0 isn't usable. The route to Scrap 0 failed, so RivalBrain marked it Blocked for 6 seconds and my policy skipped it. The rival picks up Target 1, Target becomes -1 and Visible becomes 1, reason "Cargo held: return to depot". When the scrap is delivered Task becomes Explore, reason "No eligible known pickup: visit next survey point". My prediction wasn't entirely correct as the rival doesn't ignore Scrap 0 immediately, it can only be ignored after rival attempts to reach it. Every 6 seconds it attempts to reach Scrap 0 again, which leads to a warning in the console. This resets Blocked back to 1, and refreshes the same Last failure message. |
| No usable pickup | The rival explores until match is over. Target: -1, Task: Explore, Visible: 0, Reason: "No eligible known pickup: visit next survey point". When match is over the game will end without errors in the console. | F7: The rival explores until the match is over. Target: -1, Task: Explore, Visible: 0, Reason: "No eligible known pickup: visit next survey point". When the match is over the game ends in a draw without errors in the console. |

Supplied test result, with any added tests identified: 22 pass, 0 fail (17 supplied + 5 teacher supplied tests).

Live walkthrough checked by / recording link if used:

## One correction and one independent check

The original weakness and why it happened: The starter chose the closest available candidate and ignored `Visible`/`Remembered`, so it could chase a pickup it had never seen.

What I changed and why it addresses the cause: I filter candidates on `Available && (Visible || Remembered)` before comparing distance in `DecisionPolicy.Infer`, so the rival only selects pickups it actually knows about. This does not guarantee reachability; route failures are handled in Stage 4.

My counterexample or experiment: prediction, observed result, and one limitation: I predicted that changing the Blocked filter to the IsEligible filter wouldn't change the behavior of the rival, and only make the code simpler and easier to read. I observed the rival after the change, but it now completely ignored all non-blocked scrap (only blocked pickups counted as eligible) and explored instead. This was due to a mistake I made when writing the change. I added an ! in the if-statement by accident. As a result, the rival ignored all non-blocked scraps and explored instead, as no eligible pickups were found. This behaviour was observed in F2. The bug was visible in F2 as Target -1. One limitation of this experiment are the tests. Tests only help if they are run after every change. They check decision snapshots, so they cannot prove the scene, physics or NavMesh works.

## Collaboration and help

People I discussed or playtested with: Sam Gustafsson, Carl Gustafsson, Darren Scott, Lucas Palmhager, Casper Aaröe

Code, sources or substantive tools/AI assistance I used, and how I verified them: Claude assistance:
- Stage 2: Claude wrote the truth table in `DecisionPolicy.Infer`, and diagnosed the repeated delivery navigation warnings in F4 (`remainingDistance=Infinity` treated as `Failed`). Verified by running the EditMode tests (13/17) and observing F4. The Stage 2 filter was written by Claude without me noticing before I asked it not to edit my code, I had planned the same approach, reviewed it and can explain it.
- Stage 3: I wrote the retention loop in `DecisionPolicy.Infer` myself after hints from Claude (which file/method, where the check belongs, match on `Id`). Claude explained the F5 layout (why only one pickup is visible at start), suggested the wording of the F5 observation from what I observed, and drafted the task transitions, which I used. Verified with the EditMode tests (14/17, only `Stage4_*` failing) and by playing F5 while paused and hopping forward frame by frame.
- Stage 4: I wrote the blocked check myself. When one test still failed, Claude pointed out that the retention loop also needed it, so I added it. Claude also helped explain the F6 warnings.
- Claude also fixed typos and code formatting (backticks) in this file and `DecisionPolicy.cs`.
- Claude helped with debugging. Only identifying the bugs, I wrote the fixes myself.

I made sure Claude directly changed my files as little as possible so I could learn from it. (Except for tedious tasks such as fixing typos)

NavigationFeedback.Classify updated per teacher's instruction (unknown remaining distance no longer counts as failure).

If unfinished: what works, exact reproduction steps, and the help I need: N/A
