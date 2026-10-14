# Iluminação e materiais do ClassRoom

Revisão de 13/10/2026, aplicada ao interior aninhado de
`Assets/prefabs/Scenario/ClassRoom.prefab`, na Unity **6000.5.7f1 / URP 17.5**.
A direção segue a skill fornecida em `C:/Users/leo/Desktop/SKILL.md`.
As referências do mod clássico são [Hotel Lobby](https://www.moddb.com/mods/total-chaos/images/hotel-lobby),
[Shader implementation](https://www.moddb.com/mods/total-chaos/images/shader-implementation)
e a captura de corredor já preservada em `.utmp/TotalChaosReferences/corridor.jpg`.
As imagens das duas páginas do ModDB não puderam ser baixadas nesta execução;
a análise visual utilizou a captura local do mod. Nenhum asset foi extraído dele.

## Diagnóstico e correções

- A atmosfera anterior impunha ambiente quase preto, névoa Exp² 0,05,
  contraste +18, vinheta 0,42 e grain 0,55. Agora há base indireta discreta,
  névoa 0,012, exposição fixa em 0 EV, tonemapping Neutral, contraste +6,
  vinheta 0,16 e bloom 0,08. Grain e aberração cromática ficam em zero.
- A seleção regional tinha mudanças instantâneas e alocava arrays/ordenações
  por câmera. `TCRegionAtmosphere` conserva o sistema existente, seleciona sem
  essas alocações e oferece transição espacial de 2 m, alinhada ao Volume local.
  O ClassRoom ativa essa opção; as outras regiões mantêm seus limites existentes.
- As três lâmpadas com `TCFlickerLight` ficam desabilitadas no prefab, preservando
  seus componentes e IDs. Os tubos sobreviventes recebem luz fria direcionada
  para baixo; as janelas emitem para dentro. Intensidades das fluorescentes
  permanecem iguais; os ajustes de alcance/direção corrigem sua distribuição.
- Duas lâmpadas de outros ambientes eram consideradas para sombras através das
  paredes, causando redução automática do atlas de 2048². A opção da sala limita
  temporariamente essas sombras externas ao tier baixo, mantendo duas fontes
  principais em 512, a lâmpada de mesa em 256 e a lanterna em 1024. Os tiers
  anteriores voltam ao sair. A configuração global da pipeline e o SSAO existente
  (intensidade 0,4, raio 0,3) permanecem preservados.
- A lanterna usa Spot de 52° / núcleo de 24°, intensidade 3,2, alcance 12 m,
  atenuação nativa da URP e sombras suaves. A alternância existente com F continua.
- A sonda realtime podia capturar o céu antes da atmosfera interna e dependia
  da opção de reflexos realtime da qualidade. Agora usa cubemap estático HDR
  de 128², capturado na sala, com projeção de caixa e intensidade 0,35.
- Os materiais recuperam normal maps e variação de suavidade antes comprimida
  por um multiplicador de 0,1. As paredes externas da sala usam o PBR de alvenaria
  existente; o mosaico do piso recebe filtragem e relevo fino. Água não é metálica.
  Texturas PBR existentes de 512 px são reutilizadas com importações locais,
  trilinear, mipmaps e anisotropia 4. As cópias são necessárias porque a Unity
  guarda filtragem/resolução no asset da textura; as versões Point/128 dos outros
  mapas permanecem intactas. O mosaico original de 128 px mantém sua resolução.

Geometria chanfrada, teto quebrado, infiltrações, entulho, mobiliário, escala,
colisores, textos, pistas, corpos e scripts de gameplay foram preservados.
Não foram acrescentadas luzes de preenchimento sem fonte, polígonos ou efeitos caros.
A base indireta é uma aproximação ambiente; não foi feito bake de GI/lightmaps.

## Verificação

Os detalhes e limitações dos testes estão em [validation.md](validation.md).
Capturas comparáveis, na posição real da SampleScene:

- [Antes, corredor sem lanterna](Before-Aisle.png) / [final](Final-Aisle.png).
- [Antes, frente](Before-Front.png) / [final](Final-Front.png).
- [Antes, porta](Before-Door.png) / [final](Final-Door.png).
- [Antes, com lanterna](Before-Flashlight.png) / [final](Final-Flashlight.png).

As capturas ocultam renderizadores dos outros cenários e textos antigos externos
ao interior; conservam os textos autorais da sala na versão final. Luzes externas
continuam presentes. O teste em Play Mode usa a SampleScene completa e desabilita
scripts de gameplay para isolar a atmosfera. A verificação de rotas é independente.

## Reprodução

Trabalhe numa cópia isolada do projeto em `.utmp/BasementPreview`, sincronizada
com os assets atuais, e copie os dois arquivos C# desta pasta para `Assets/Editor`.
O runtime instalado e a fonte em `Tools/TotalChaosSkill/Unity/Runtime` devem coincidir.
Os verificadores de ClassRoom anteriores também precisam estar na cópia.

1. Preserve os assets anteriores e seu manifesto SHA-256 em
   `.utmp/ClassRoom-lighting-before`, incluindo o interior e o runtime.
2. Execute `ClassRoomLightingBuilder.Build` com `-batchmode -quit`.
   O builder reutiliza caminhos existentes e não recria mapas.
3. Execute `python Tools/ClassRoom/Lighting/install_classroom_lighting.py`:
   prepara o YAML na cópia, conservando todos os IDs e blocos de gameplay.
4. Execute `ClassRoomLightingVerifier.VerifyFinal` com `-batchmode -quit`,
   log `.utmp/ClassRoom-lighting-final.log`.
5. Execute `ClassRoomLightingVerifier.RunPlayMode` com `-batchmode`, sem `-quit`:
   o runner encerra o editor ao terminar. Log `.utmp/ClassRoom-lighting-play.log`.
6. Execute `ClassRoomHorrorVerifier.VerifyAndRender` com `-batchmode -quit`,
   log `.utmp/ClassRoom-lighting-gameplay.log`.
7. Execute o instalador com `--install`. Ele exige os marcadores dos testes,
   rejeita erros e alterações concorrentes e transfere somente os assets da revisão.

Os geradores antigos de atmosfera restabelecem a direção Point/128 anterior;
execute esta revisão depois deles para conservar o acabamento atual da sala.
