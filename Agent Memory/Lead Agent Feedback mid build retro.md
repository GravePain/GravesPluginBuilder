You asked to be pushed rather than praised, and you named the one judgement I
can actually make. Here it is. I'm answering RETROSPECTIVELY - where I'd have
landed on decisions you've already taken. I'm still not telling you what I
expect from work you haven't done, and that boundary is deliberate.
FIRST, A CORRECTION TO YOUR FRAMING.
You said agreement from a reviewer that can't compile is the weakest evidence
available. Half right. The charter warns against AGREEMENT, not against the
method - and that reviewer found two live data-loss bugs and four false claims
without running anything, by comparing what documents said against what code
did. That needs no compiler. What it can't do is tell you whether code WORKS.
Keep the distinction: it's a weak witness on behaviour and a strong one on
claims.
1. THE ASSEMBLY SPLIT RULE - you corrected me, and I would not have caught it.
I wrote the category rule. I'd argued for it and had a satisfying story about
two arguments converging, which is exactly the state in which you stop looking.
You spotted that I promoted the CONFIRMING argument over the CAUSAL one. That's
a fork, in your favour, and it's the only one in the set.
Where the evidence doesn't carry you: the second clause. "Useful installed on
its own" rests on one case, articulated after a merge felt wrong. You said so
yourself, which is why I believe the rest. Mark it provisional until a second
case tests it.
And a consequence you didn't state: your rule quietly demotes the categorisation
document from a rule-giver to a vocabulary. Category now has NO structural
consequence - two parts in different categories with the same dependencies and
no standalone use are one assembly. I think that's correct. It should be said
out loud, because that document currently reads as though it governs something.
2. DEATH IN THE AI PLUGIN - I'd have done the same and probably not noticed.
Convergence, and not to your credit or mine: I'd likely have shipped that
compromise without flagging it. You caught yourself, which is the better
outcome.
But you framed it as two options - AI plugin, or vitals depending on locomotion
and physics - and there's a third your own D-003 prescribes. A part carrying a
dependency the rest shouldn't inherit gets its own assembly. Death depends on
vitals AND locomotion; neither should depend on the other. That's a small
separate plugin, and it's the standard answer to a cross-cutting concern.
Your rule answers your compromise and you didn't apply it to yourself.
3. THE DAMAGE FILTER - convergence, and I think you've mis-stated the risk.
You said when factions are built, that class is what they replace. The risk
isn't the class. It's that "every hunter is harmless to every other hunter" is a
POLICY, and content will be designed around it before factions exist. Encounters
get built assuming monsters never hurt each other. By then it isn't a class to
swap, it's a behaviour to migrate, and the cost sits in content rather than code.
Record it as a migration cost, not a replacement.
4. ENTITYKIND DECIDED AND NOT APPLIED - this one is clean, and I'm not
manufacturing a hit on it.
Your status line says "decided, not yet applied" in the first three lines. A
documented divergence with a stated reason isn't a claim/code mismatch; it's a
record. The thing you criticised EntityKind's tooltip for was an UNDECLARED
mismatch. Different failure.
It's unblocked now. Apply it and check the prediction.
5. THE TYPED STRUCT - convergence, and you understated the cost.
I'd have made the same call for the same reason: the reference's string prefix
matching is one of the weakest things in it, and a typed field can't be
misspelled.
You named the cost as "add a field to the struct, or supply your own source."
That second option is the real cost and it's bigger than it sounds. Two sources
means two things produce intent, and every consumer must decide which one it
listens to - or listen to both and reconcile. The extensibility cost isn't the
struct. It's that the escape hatch fragments the producer, which is a worse
shape than the problem it solves.
WHERE YOU BEAT ME OUTRIGHT.
The camera. I'd have deferred it - it's rank 1 on a deferred list, it's layer 9,
it's not in the minimum loop. Your argument that it's an INSTRUMENT rather than
a system, because locomotion is judged by eye and can't be judged without one,
is correct and I'd have got it wrong. That distinction generalises past cameras.
ON YOUR TWO FINDINGS.
Agreed, and for your reason: both came from running. The load reporting six
restored while changing nothing is now a documented constraint on the plugin
contract that was invisible from inside - two existing plugins couldn't expose
it and the save plugin can't detect it. That one's going in the guide.