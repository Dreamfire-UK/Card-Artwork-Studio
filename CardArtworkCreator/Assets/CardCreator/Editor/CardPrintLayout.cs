using System.Collections.Generic;
using System.Text;

namespace CardCreator.Editor
{
    internal sealed class CardLayoutReport
    {
        public bool rulesOverflow;
        public bool flavorHidden;
        public string Message => rulesOverflow
            ? "Rules exceed the printable area at the minimum readable size. Shorten the rules before exporting."
            : flavorHidden ? "Flavour text is hidden to keep the rules readable." : "";
    }

    internal static class CardPrintLayout
    {
        internal static string Cost(CardDefinition card)
        {
            if (card.kind == CardKind.Evolution) return "Evolution Deck";
            var costs = new List<string>();
            if (card.essence != Essence.None) costs.Add("1 " + card.essence);
            if (card.secondEssence != Essence.None) costs.Add("1 " + card.secondEssence);
            if (card.genericEssenceCost > 0) costs.Add(card.genericEssenceCost + " any");
            return costs.Count == 0 ? "0" : string.Join(" + ", costs);
        }

        internal static string Rules(CardDefinition card, bool guardStance)
        {
            var lines = new List<string>();
            if (card.guard) lines.Add("Guard");
            if (card.promote)
                lines.Add("Promote — " + Clean(card.promoteCondition));
            if (card.attackArrows != CardArrow.None)
            {
                string effect = guardStance && !string.IsNullOrWhiteSpace(card.guardArrowEffect)
                    ? card.guardArrowEffect : card.arrowEffect;
                if (!string.IsNullOrWhiteSpace(effect)) lines.Add(Clean(effect));
            }
            if (card.kind == CardKind.Evolution)
            {
                string source = card.evolvesFrom != null ? card.evolvesFrom.cardName : "[Choose base creature]";
                lines.Add("Triumph — " + source + " · " + card.requiredScars + " Scars.");
                if (!string.IsNullOrWhiteSpace(card.triumphQuest)) lines.Add("Quest — " + Clean(card.triumphQuest));
                if (!string.IsNullOrWhiteSpace(card.onEvolution)) lines.Add("On evolution — " + Clean(card.onEvolution));
                if (!string.IsNullOrWhiteSpace(card.evolutionOngoing)) lines.Add(Clean(card.evolutionOngoing));
            }
            if (card.abilities != null)
                foreach (var ability in card.abilities)
                {
                    if (ability == null || string.IsNullOrWhiteSpace(ability.effect)) continue;
                    var text = new StringBuilder();
                    if (ability.oncePerTurn && ability.trigger != CardTrigger.OncePerTurn)
                        text.Append("Once per turn, ");
                    var trigger = Trigger(ability.trigger);
                    if (trigger.Length > 0) text.Append(trigger).Append(" — ");
                    if (!string.IsNullOrWhiteSpace(ability.condition)) text.Append(Clean(ability.condition)).Append(": ");
                    text.Append(Clean(ability.effect));
                    lines.Add(text.ToString());
                }
            return string.Join("\n", lines);
        }

        internal static string Trigger(CardTrigger trigger)
        {
            switch (trigger)
            {
                case CardTrigger.Passive: return "";
                case CardTrigger.WhenPlayed: return "When played";
                case CardTrigger.WhenPromoted: return "When promoted";
                case CardTrigger.WhenMoved: return "After moving";
                case CardTrigger.WhenStanceChanged: return "After changing stance";
                case CardTrigger.WhenAttacking: return "When attacking";
                case CardTrigger.WhenGuarding: return "When guarding";
                case CardTrigger.AfterBattle: return "After battle";
                case CardTrigger.WhenZoneEmpties: return "When a zone becomes empty";
                case CardTrigger.WhenEvolved: return "On evolution";
                case CardTrigger.Action: return "Action";
                case CardTrigger.OncePerTurn: return "Once per turn";
                default: return trigger.ToString();
            }
        }

        private static string Clean(string text) => (text ?? "").Trim();
    }
}
