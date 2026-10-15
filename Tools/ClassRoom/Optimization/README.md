# ClassRoom: reparo de texturas e otimização — 15/10/2026

## 1. Diagnóstico

Aplicados o `PROMPT_MESTRE.md` e a skill enviados pelo usuário. Alvo:
`Assets/prefabs/Scenario/ClassRoom.prefab`, incluindo o nested prefab
`Assets/Art/ClassRoom/AbandonedClassRoomInterior.prefab`.

Unity **6000.5.7f1**, URP **17.5.0**. Validação em uma cópia isolada
`.utmp/ClassRoomPreview`, usando Windows, Direct3D 12 e AMD Radeon RX 6400.
A sessão aberta do projeto não foi fechada. Isso não estabelece o hardware-alvo
do jogo nem constitui um benchmark de Player.

Foram encontradas **69 referências de textura quebradas em 22 materiais**.
Os arquivos PNG existem, mas os GUIDs gravados nesses slots não correspondem
aos assets atuais. Isso afetava albedo, normal, máscaras, displacement e alguns
slots legados `_MainTex`. Também havia mapas importados como imagens sRGB e
keywords de normal/metallic/AO desligadas. O reparo foi incluído após o pedido
explícito do usuário e altera a aparência para restaurar as superfícies.

O mapeamento completo está em [texture-bindings.json](texture-bindings.json).
Madeira e piso usam os conjuntos `wood_table_worn` e `worn_tile_floor`.
Tecidos, sangue, livros e alvenaria usam as cópias existentes em
`Assets/Art/ClassRoom/Lighting/Textures`, conforme as famílias dos geradores em
`Tools/TotalChaosStyle/TotalChaosStyleBuilder.cs` e do builder de iluminação.
Não foram geradas imagens novas.

| Inventário da instância completa, incluindo objetos inativos | Antes | Depois |
| --- | ---: | ---: |
| Transforms | 835 | 835 |
| MeshRenderers / MeshFilters | 607 / 599 | 607 / 599 |
| Materiais distintos | 41 | 41 |
| Meshes distintas usadas pelos MeshFilters | 584 | 577 |
| Memória dessas meshes no Editor, bytes | 20.671.632 | 20.512.320 |
| Vértices nas meshes distintas | 158.803 | 157.555 |
| BoxColliders / Rigidbodies | 126 / 1 | 126 / 1 |
| Luzes / luzes ativas com sombras | 19 / 3 | 19 / 3 |
| Materiais com `_TC_SHARED_MASK` | 0 | 12 |

A redução medida nas meshes é **159.312 bytes, aproximadamente 156 KiB** no
Editor. Não houve redução da geometria desenhada; a contagem de vértices acima
conta cada asset distinto uma vez. Um MeshFilter vazio sem renderer ativo no
prefab da porta foi reconhecido como existente, sem removê-lo.

## 2. Ranking de otimizações

| Prioridade / problema | Gravidade e impacto esperado | Risco / esforço | Como comprovar |
| --- | --- | --- | --- |
| 1. Referências e importação de texturas | Falha visual confirmada; restaura PBR | Mudança visual solicitada; médio | Resolver os slots e comparar capturas |
| 2. Leituras repetidas da mesma máscara | Potencial de reduzir trabalho de textura por fragmento | Baixo; baixo, reutiliza implementação existente | Shader A/B, GPU Profiler e frame time GPU no Player |
| 3. Meshes idênticas serializadas separadamente | Redução pequena de memória confirmada no Editor | Baixo; baixo | Igualdade dos dados e Memory Profiler no Player |
| 4. Submissão de renderers, luzes e overdraw | Possíveis gargalos seguintes; sem medição para classificá-los | Depende do profiling | CPU/GPU Profiler e Frame Debugger durante o jogo |

Restaurar mapas e recursos PBR pode aumentar custo de GPU e memória em relação
ao estado quebrado. Por isso a comparação de otimização usa a etapa **repaired**
como referência, com as texturas já restauradas.

## 3. Implementação

- **24 materiais**: 22 com referências reparadas e dois adicionais com recursos
  do shader reativados. Cores, parâmetros, UV scale/offset, transparência, estados
  de renderização e atribuições dos renderers foram preservados.
- **14 arquivos `.meta` de textura**: normal maps importados como Normal Map e
  dados escalares como lineares. GUIDs, resolução, compressão, filtragem e mipmaps
  foram preservados. Texturas já válidas foram mantidas, inclusive referências
  existentes a cópias recuperadas no Basement; seus importadores não foram editados.
- **12 materiais Total Chaos**: ativação de `_TC_SHARED_MASK`. A implementação
  já existente em `TotalChaosSurfaceInput.hlsl` reutiliza uma leitura para metallic,
  smoothness, AO e cavidade quando os três slots apontam para a mesma Texture.
  Três leituras lógicas passam a uma nos passes de cor correspondentes; isso
  não mede instruções GPU nem FPS. O shader e o conversor compartilhados, que já
  tinham alterações anteriores a esta tarefa, não receberam novas alterações.
- **AbandonedClassRoomInterior.prefab**: sete referências de MeshFilter usam
  assets com conteúdo serializado idêntico, exceto o nome. Vértices, índices,
  normais, tangentes, UVs, bounds e submeshes foram comparados. Os assets originais
  permanecem existentes. Os demais campos foram preservados na etapa de otimização;
  a remoção posterior dos textos está descrita abaixo.
  O prefab raiz ClassRoom e o nested prefab da porta permanecem iguais.
- **ClassRoomPerformanceAudit.cs**, em `Assets/TotalChaos/Editor`: auditoria,
  reparo, otimização e captura por API Unity. Os resultados ficam fora de Assets.
  Menus: `Tools > Total Chaos > Repair ClassRoom Textures` e
  `Tools > Total Chaos > Optimize ClassRoom Assets`.

A edição foi aplicada e carregada pela Unity isolada. A integração final conservou
somente os campos necessários, excluindo alterações incidentais de serialização.
Os **3.059 fileIDs** dos três prefabs foram preservados. Nenhuma alteração de
hierarquia, luz, collider ou script de gameplay foi necessária.

## 4. Testes e rollback

- Scripts compilados e capturas renderizadas na Unity instalada, sem erro de
  shader nas variantes exercitadas.
- Quatro vistas em 1280 × 720: corredor entre carteiras, frente, porta e lanterna;
  pipelines PC e Mobile, ambos executados no computador.
- **Oito comparações A/B do shader idênticas pixel a pixel**, alternando a keyword
  na mesma sessão após o reparo.
- Comparação final completa `repaired` → `after`, recarregando os assets finais:
  **oito capturas idênticas pixel a pixel**, incluindo a consolidação das meshes.
- **163 slots de textura não nulos** nos materiais das dependências resolvem para
  arquivos existentes. As 69 referências reparadas foram conferidas individualmente.
- Verificação dos fileIDs, equivalência das sete meshes, preservação dos outros
  campos dos materiais e dos importadores, e verificação de aplicação reversa do patch.
- O resultado do Development Build Windows está em [validation.txt](validation.txt).

Executar a partir da raiz: `py Tools/ClassRoom/Optimization/verify_classroom.py`.
O script usa backups/evidências em `.utmp/ClassRoomOptimization` e Pillow para
comparar as imagens. [Comparação visual antes/depois](comparison.png).

Para reverter os 39 assets alterados, executar primeiro
`git apply --check -R Tools/ClassRoom/Optimization/changes.patch` e, se passar,
`git apply -R Tools/ClassRoom/Optimization/changes.patch`.
O patch não inclui alterações anteriores desta sessão nem remove os novos arquivos
de auditoria/relatório. Há backups dos arquivos em `.utmp/ClassRoomOptimization/source-before`.
Não sobrescrever edições posteriores ao reverter.

Ainda é necessário medir Player antes/depois no hardware-alvo, com a mesma câmera,
resolução e carga, após aquecimento: CPU main/render thread e GPU em **ms**, GC,
memória, SetPass, batches/draw calls, triângulos e overdraw. Validar Play Mode,
percurso, porta com chave, combate e transições de atmosfera. Não foram testados
dispositivo móvel, XR ou bake de lightmaps.

Referências: [edição isolada de prefabs na Unity 6000.5](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/PrefabUtility.LoadPrefabContents.html)
e [CBUFFER/SRP Batcher na URP](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/shaders-in-universalrp-srp-batcher.html).
O cálculo do shader foi conferido com `LitInput.hlsl` da URP 17.5 instalada.

## 5. Veredito

**Confirmado:** referências de textura reparadas, importação corrigida, recursos
PBR reativados, sete meshes distintas a menos, aproximadamente 156 KiB a menos
nas meshes medidas no Editor e shader A/B equivalente nas oito capturas.

**Potencial:** menor custo de leitura de máscaras nos 12 materiais compatíveis.
**Não medido:** ganho de FPS/frame time, memória total do Player, draw calls e
comparação de variantes/tamanho de builds. A reparação visual e a otimização são
etapas distintas nos inventários e nas capturas.

## Remoção posterior de textos

A pedido do usuário, os oito textos visíveis da sala foram esvaziados e seus
renderizadores desativados. Objetos, componentes e fileIDs foram preservados.
Essa mudança foi validada na Unity e em oito capturas; o build Windows e as
comparações A/B acima registram a etapa anterior à remoção.
[Detalhes, captura final e reversão somente dos textos](../TextRemoval/README.md).
O patch agregado deste diretório já inclui a remoção dos textos.
