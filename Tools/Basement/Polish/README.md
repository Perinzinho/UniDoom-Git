# Basement: decoração das arenas

Unity 6000.5.7f1 / URP 17.5. Direção visual baseada na skill
`total-chaos-visual-direction` fornecida pelo usuário e nas referências do
[mod clássico](https://www.moddb.com/mods/total-chaos/images/hotel-lobby).

A pré-arena recebe dois núcleos de manutenção sobre as plataformas existentes,
estantes, carrinhos, caixas de ferramentas e barris com texturas reais. Cabos,
mangueiras, treliças e entulho localizado dão profundidade ao salão. Os quatro
barris anteriores recebem modelos detalhados, mantendo seus colisores.

A sala final recebe acabamentos metálicos rebitados em torno do altar, luminárias físicas,
alimentação de pressão e grelhas de serviço laterais. A decoração acompanha a
âncora do altar, incluindo o deslocamento aplicado pela cena principal.

Os novos modelos usam URP Lit, albedo e normal GL em 2K, AO e metallic/smoothness.
O mapa ARM original é preservado; a máscara Unity gerada usa R=metallic, G=AO,
B=roughness e A=1-roughness. Texturas têm mipmaps, filtro trilinear e anisotropia 4.
As superfícies autorais reutilizam o shader PBR e as texturas existentes.
Treliças, cabos e acabamentos são consolidados por material; os modelos externos
compartilham suas malhas. Quatro luzes locais novas não projetam sombras.

Foram recuperadas 23 referências de texturas que já estavam quebradas no estado
inicial. `RecoveredTextures` contém cópias dos arquivos existentes com os GUIDs e
as configurações de importação originais recuperados do Git. Os `.meta` atuais
do usuário permanecem intactos. `recovered-textures.json` registra cada origem,
GUID restaurado, hash e licença. A recuperação também resolve materiais de outras
salas que já referenciavam esses mesmos GUIDs ausentes.

## Modelos gratuitos

Todos são da Poly Haven, sob [CC0-1.0](https://polyhaven.com/license), sem marca
d'água. O arquivo `Assets/Art/Basement/ArenaPolish/asset-sources.json` registra
autores, URLs oficiais dos downloads e hashes SHA-256.

- [Industrial Storage Cart](https://polyhaven.com/a/industrial_storage_cart)
- [Barrel 03](https://polyhaven.com/a/barrel_03)
- [Metal Tool Chest](https://polyhaven.com/a/metal_tool_chest)
- [Steel Frame Shelves 02](https://polyhaven.com/a/steel_frame_shelves_02)

## Reprodução e reversão

`prepare.mjs` cria um snapshot e uma cópia isolada em `.utmp/BasementPreview`.
Requer que o Editor principal esteja fechado. `download.ps1` baixa os arquivos
pelos URLs retornados pela API oficial e confere MD5. Copie os quatro scripts
anteriores de `Tools/Basement` usados por `prepare.mjs`, `BasementSurfaceBuilder.cs` e
`BasementPolishBuilder.cs` para `Assets/Editor` da cópia isolada.

Execute `BasementPolishBuilder.Inspect` para as capturas anteriores. Quando houver
GUIDs de texturas ausentes, execute `recover-textures.mjs` antes da geração e
`BasementPolishBuilder.Build` para gerar os assets. Rode `node
Tools/Basement/Polish/install.mjs` para mesclar somente filhos adicionados e a
visibilidade dos renderizadores substituídos no YAML. Execute então
`BasementPolishBuilder.Verify`; depois, `install.mjs --install` exige logs de
sucesso e ausência de alterações concorrentes antes de copiar o resultado.

O snapshot está em `.utmp/BasementPolish/before`. Restaure o `Basement.prefab`
desse snapshot para desfazer a decoração; os modelos e materiais anteriores
permanecem no projeto. A cena principal, player, scripts e colisores antigos
não são gravados pelo instalador.

Capturas `Current-*` e `FinalPolish-*` usam as mesmas câmeras e atmosfera real.
`validation.txt` registra os testes efetivamente executados. FPS/GPU e ritmo do
combate no hardware alvo precisam de playtest manual.

## Revisão visual

Checklist da skill: 21/24 nas capturas, com pontos parciais em desgaste causal,
legibilidade dos detalhes e desempenho. A arquitetura original
ainda tem volumes simples e textos sobrepostos ao fundo. Esta revisão
concentra o ganho em decoração, modelos e acabamento do altar. O desempenho foi
avaliado pelo orçamento de geometria, materiais e luzes; não por uma medição de FPS.
Os dois núcleos novos somam 250.127 triângulos e 53 slots de material; os quatro
barris substitutos acrescentam 5.892 triângulos e quatro slots. As rotas foram
testadas com o CharacterController real, e os 12 spawns têm pelo menos 1,1 m de
folga em relação aos novos colisores desses núcleos.
