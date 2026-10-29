# Card Creator

Open **Tools > Card Creator > Open Card Creator** in Unity. Create a `CardDefinition` asset, set its card fields, and drag the character within the preview. The **Artwork Scale** field controls its size. Export produces a 750 × 1050 PNG (5:7 card ratio).

Artwork and backgrounds extend across the full card. Text sits over a translucent shaded overlay, surrounded by fine metallic linework and Essence accents. Directional markers are drawn as pointed insets on the outer frame; the Guard preview rotates them clockwise.

The Essence fields, board roles, abilities, arrow directions, and Triumph conditions are saved as card authoring data. Gameplay code must interpret this data when the card game is implemented. Guard arrows are calculated by rotating the Attack arrows 90 degrees clockwise.

The four backgrounds in `Backgrounds/` were created specifically to match the project's Ækashics battler illustrations. They are the defaults for Grove, Gloom, Ember, and Current when a card has no custom background selected. The earlier pixel-art backgrounds from the online CC0 bundle were removed after the style review.
