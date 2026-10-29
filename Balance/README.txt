FIVE ROADS — PROTOTYPE DECK LABORATORY

Rootbound Court and Night Brood each contain 20 main-deck cards and one evolution.
The Unity card assets are the authoring source. Alternate Briar Warden artworks
are variants, not additional copies. Deck-breakdown.html lists every exported
field and artwork reference; cards/ contains both stance previews.

RUN FROM THE REPOSITORY ROOT (Python 3.10+, no external packages)
  python -m unittest discover -s Balance -v
  python Balance/balance.py Balance/decks.json --out Balance/Reports --pairs 500

After editing Unity cards, use Tools > Card Creator > Balance > Export Decks and
Card Previews and select the repository's Balance folder. Re-run the commands.
Stats and arrows are read from that export. Effects use explicit named-card
adapters, NOT a natural-language rules engine. A changed effect fails validation
against supported-rules.json. Update the adapter AND its tests before updating
that signature. Unrecognised cards and mixed-Essence costs are rejected.
--rules path.json overrides the prototype profile; --seed changes paired seeds.
--sweep compares Grade 2 defence candidates without changing authored cards.

EXPERIMENTAL RULES — NOT YET CONFIRMED AS THE FINAL GAME RULEBOOK
20 life; opening hand of five; five columns and two rows per player.
Draw one each turn, except the first player's first turn. Empty draw pile loses.
One free rear placement each turn. The bot keeps at most three rear creatures;
this is a bot resource policy, not a board-capacity restriction.
Ready rear creatures exhaust to supply their printed Essence. Front deployment
costs one matching Essence plus the printed generic cost, and one action.
Two actions per turn: front deployment, a one-column front move, or stance change.
All arrivals are exhausted. Ordinary moves preserve readiness. Only Sprout and
Skitter can Promote; Spectre's follow-up movement is front-to-front only.
Guard stance prevents attacking. A ready front Guard may intercept an attack in
an adjacent column, including an open lane, and exhausts when intercepting.
Attack down your own column. Combat damage is simultaneous and persists.
Defence is the lethal damage threshold. Positive damage survived grants one Scar
per combat. Zero damage and lethal damage do not grant Scars.
Arrows target neighbouring zones in board space, rotate 90 degrees clockwise in
Guard stance, mirror with player facing, and never wrap across an edge.
Effects only apply to their actual arrowed zones. Demon weakens arrowed enemies
during its controller's attacks; its Guard arrows instead support allies.
Web consumes its marker on the next entry and skips that creature's next ready
step. This explicit duration makes Web matter when all normal arrivals exhaust.
Triumph needs two Scars plus the printed quest. One evolution copy per player;
damage, exhaustion and Scars remain. Yggdrasil's printed healing still applies.
For the prototype, 'end an opponent's battle' is checked after each combat.
Citadel rescue cancels the lethal damage packet and moves the creature to an
empty rear zone exhausted, keeping its earlier damage. This is an explicit
provisional ruling to resolve the source text's otherwise lethal retained damage.
No hand traps, locks, chain system, rear attacks, or automatic universal promotion.
An 80-turn cap scores a draw. No arbitrary additional abilities are simulated.

WHAT THE TESTS MEAN
Paired seeds swap the two decks between first and second seat. Confidence
intervals cluster by seed pair. Mirror matches check seat symmetry; 50% mirror
results alone are not evidence of good gameplay. The bot uses current board
information, no opposing hand knowledge, and a simple greedy placement policy.
Human play, alternate strategies, timing disputes and resource-system changes
can produce different results. A close bot matchup is only a playtest starting
point. Inspect the board replay and metrics as well as the win rate.

BALANCE PASS
Initial Night Brood was substantially behind Rootbound in this rules profile.
Its Grade 1 creatures gained 1 power, Grade 2 creatures gained 1 defence, and
Broodgate Demon gained 1 defence. Roles, quantities, quests and two-Scar Triumph
requirements were retained. Tuning files record exploratory comparisons.
The final independent seed run is recorded in run-manifest.json and matchups.json.
Queen's Triumph remains much rarer than Citadel's: this needs human playtesting.
Do not silently lower the Scar requirement to conceal that difference.

CARD TEXT AND LAYOUT
Numbered effects use OCG-style organisation; condition: activation; resolution
punctuation follows PSCT principles where applicable. No costs are invented just
to include a semicolon. AUTO = triggered, ACT = activated, CONT = continuous.
These are Vanguard-inspired timing labels, not imported Vanguard mechanics.
Guard/Promote use lavender emphasis; Triumph/Scars gold. Labels also spell out
their meaning so colour is not the sole distinction. The full-width rules panel
has adaptive height, 87% opacity, and a fixed readable font rather than shrinking
rules to hide overflow. Flavour yields to rules. Overflow is reported on export.
Sources: https://www.yugioh-card.com/en/play/psct/psct-3/
https://en.cf-vanguard.com/wordpress/wp-content/uploads/vg_first_guide_v1.6.pdf

ARTWORK
18 creature cutouts were AI-enhanced from the project's licensed Ækashics sprites.
They are high-resolution reinterpretations, not original detail recovered by
interpolation. Original sprites remain in place. The card face retains artist
credit. EnhancedArtwork/artwork-provenance.json maps every card to its original.
Textures import without mipmaps or block compression, with a 4096 size limit.
Card previews are actual Unity renders at 750 x 1050 pixels.
