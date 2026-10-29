using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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
            if (card.guard) lines.Add("[CONT] Guard.");
            if (card.promote) lines.Add("[AUTO] Promote — " + Punctuate(card.promoteCondition));
            string arrow = guardStance && !string.IsNullOrWhiteSpace(card.guardArrowEffect)
                ? card.guardArrowEffect : card.arrowEffect;
            if (card.attackArrows != CardArrow.None && !string.IsNullOrWhiteSpace(arrow))
                lines.Add((HasTiming(arrow) ? "[AUTO] " : "[CONT] ") + Punctuate(arrow));
            if (card.kind == CardKind.Evolution)
            {
                string source = card.evolvesFrom != null ? card.evolvesFrom.cardName : "[Choose base creature]";
                lines.Add("Triumph — " + source + " with " + card.requiredScars + " Scars. " + Clean(card.triumphQuest));
                if (!string.IsNullOrWhiteSpace(card.onEvolution)) lines.Add("[AUTO] On evolution: " + Clean(card.onEvolution));
                if (!string.IsNullOrWhiteSpace(card.evolutionOngoing)) lines.Add("[AUTO] " + Punctuate(card.evolutionOngoing));
            }
            if (card.abilities != null)
                foreach (var ability in card.abilities)
                {
                    if (ability == null || string.IsNullOrWhiteSpace(ability.effect)) continue;
                    string effect = Clean(ability.effect);
                    bool timed = HasTiming(effect);
                    bool action = ability.trigger == CardTrigger.Action || ability.trigger == CardTrigger.OncePerTurn;
                    string label = action ? "[ACT] " : ability.trigger == CardTrigger.Passive && !timed ? "[CONT] " : "[AUTO] ";
                    string trigger = Trigger(ability.trigger);
                    // Authored timing takes precedence over generic enum labels.
                    string text = timed ? Punctuate(effect) : (trigger.Length > 0 ? trigger + ": " : "") + effect;
                    if (!string.IsNullOrWhiteSpace(ability.condition)) text = Clean(ability.condition) + ": " + text;
                    if (ability.oncePerTurn && ability.trigger != CardTrigger.OncePerTurn && !text.StartsWith("Once per turn")) text = "Once per turn, " + text;
                    lines.Add(label + text);
                }
            for (int i = 0; i < lines.Count; i++)
                lines[i] = (lines.Count > 1 ? "(" + (i + 1) + ") " : "") + lines[i];
            return string.Join("\n", lines);
        }
        internal static string RichRules(CardDefinition card, bool guardStance)
        {
            string text = Rules(card, guardStance).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
            text = Regex.Replace(text, @"\[(AUTO|ACT|CONT)\]", match => {
                string type = match.Groups[1].Value;
                string color = type == "AUTO" ? "F4B08E" : type == "ACT" ? "90C9EF" : "B6D996";
                return "<color=#" + color + "><b>[" + type + "]</b></color>";
            });
            text = Regex.Replace(text, @"\b(Guard|Promote|Triumph|Scars|Web)\b", match =>
                "<color=#" + (match.Value == "Triumph" || match.Value == "Scars" ? "EBCB82" : "C5B2F3") + "><b>" + match.Value + "</b></color>");
            return text;
        }
        private static bool HasTiming(string text) => Regex.IsMatch(Clean(text), @"^(When |After |If |Once per turn,)");
        private static string Punctuate(string value)
        {
            string text = Clean(value);
            // Convert only a timing-clause comma; never invent an activation cost.
            return Regex.Replace(text, @"^((?:Once per turn, )?(?:When|After|If) [^,:]+), ", "$1: ");
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
