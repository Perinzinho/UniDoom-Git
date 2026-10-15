# Símbolo do altar

Textura: `Assets/Art/Basement/ArenaPolish/Textures/AltarReferenceSeal.png`.
Material: `Assets/Art/Basement/ArenaPolish/Materials/AltarReferenceSeal.mat`.

Imagem de referência fornecida pelo usuário no chat. Preparada com a ferramenta
integrada `imagegen`, em modo de edição com fundo transparente. Não foi usado
o fallback CLI. O resultado foi copiado para o projeto.

O material URP Lit usa alpha clipping, emissão discreta, mipmaps com preservação
de cobertura e filtragem trilinear. A escala de UV preserva a proporção quadrada
no painel existente e compensa os dois eixos invertidos nos UVs da face antiga.
Apenas uma referência de material foi trocada no prefab;
o material e a textura antigos permanecem disponíveis.

Backup desta troca: `.utmp/BasementSymbol/Basement-before.prefab`.
Prévia: `Altar-NewSymbol.png` e `Altar-NewSymbol-Wide.png` nesta pasta.

## Prompt utilizado

Use case: background-extraction. Edit target: the user's most recent attached image, a very intricate dark red alchemical seal on black. Extract that EXACT supplied symbol as a transparent PNG game texture. Remove only the black background and black negative spaces; preserve the red cross, triangles, concentric circles, every ornamental loop, thin lines, fine distressed strokes, geometry, proportions, orientation and dark oxblood red color exactly as supplied. Keep the full emblem within the image, centered on a square canvas with the same modest margins; no cropping. Flat original artwork only. Do not redesign, simplify, add symbols, add text, change colors, add glow or add shading. All empty space must be truly transparent, including enclosed negative spaces. Output a faithful clean cutout of the input.
