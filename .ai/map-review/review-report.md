# Revisão dos cenários — 20/10/2026

7 cenas identificadas; 6 elegíveis; 1 excluída (`DirectorRoom.unity`). 6 auditadas; 3 com referências de textura corrigidas; 6 carregadas e inspecionadas na Unity; 0 aprovadas integralmente; 6 com pendências de validação. Autoria por IA não confirmada: foram incluídas todas as cenas elegíveis, inclusive Basement fora da lista atual do build.

A exclusão inclui a subárvore `Environment/SampleScenario/DirectorRoom (2)`. Os 347 arquivos da pasta DirectorRoom e os sete arquivos .unity têm os mesmos SHA-256 iniciais. Nenhum prefab, script de gameplay, shader, perfil de luz ou configuração global foi alterado. O único conteúdo preexistente fora do Git era `.codex/reviewer/`, preservado.

## Diagnóstico e mudanças

Foram comprovadas 56 referências a 14 GUIDs antigos de textura em 21 materiais usados por renderizadores do porão (15 materiais), cilindro (6) e cena integrada (21, os mesmos recursos). O histórico Git mostra trocas de GUID anteriores à revisão. A identidade das imagens foi verificada pelo SHA-256 do objeto Git LFS, não apenas pelo nome.

- Nove referências lógicas de imagem reutilizam assets atuais idênticos; oito texturas foram movidas de ClassRoom/Lighting/Textures para `Assets/_Project/Global/Textures/MapSurfaces/` via AssetDatabase.MoveAsset, junto dos importadores originais. Paint_Albedo reutiliza o Metal_Albedo já global, com conteúdo idêntico.
- Cinco mapas necessários foram recuperados do LFS local para MapSurfaces: Organic_Albedo, Organic_Normal, Organic_MetalRough, Paint_Normal e Paint_MetalRough. Seus GUIDs e importadores históricos foram preservados. Paint_Normal tem pixels idênticos a um Metal_Normal existente, mas esse asset é importado como cor/sRGB, incompatível com normal map; preservou-se a importação de normal sem alterar o consumidor de cor.
- Os 17 materiais editados têm apenas 46 substituições de GUID. Outros quatro materiais voltam a resolver seus mapas pelos cinco assets recuperados. FileIDs, parâmetros de cor, roughness/smoothness, tiling, keywords e render states foram preservados. A aparência pretendida dos materiais passa a ter as texturas que faltavam; equivalência visual no jogo ainda não foi medida.
- Nenhuma pasta antiga Assets/Art, Assets/prefabs ou Assets/TotalChaos foi recriada. Os novos assets são dependências efetivas do jogo, não backups ou quarentena.

## Evidências por cena

Contagens do Editor incluem objetos inativos e prefabs expandidos. Luzes: total / realtime ativas com sombras. Não representam custo de um frame nem benchmark.

| Cena | Objetos | Renderizadores | Luzes | Materiais reparados | Resultado |
|---|---:|---:|---:|---:|---|
| Basement | 1588 | 698 | 75 / 5 | 15 | PASS técnico; pendente integral |
| ClassRoom | 848 | 607 | 20 / 4 | 0 | PASS técnico; pendente integral |
| Cylinder | 2645 | 917 | 33 / 3 | 6 | PASS técnico; pendente integral |
| EntranceGarden | 1174 | 629 | 40 / 3 | 0 | PASS técnico; pendente integral |
| Library | 10803 | 2000 | 14 / 5 | 0 | PASS técnico; pendente integral |
| SampleScene | 17033 | 4872 | 178 / 16 | 21 | PASS técnico; pendente integral |

A direção de cada ambiente e os perfis de atmosfera estão registrados no manifesto. Há diferenciação serializada entre sala de aula, biblioteca, jardim noturno, cilindro e sub-regiões do porão; isso não comprova composição ou legibilidade final. Não foram inventadas notas visuais nem aplicados ajustes estéticos universais.

## Pendências e ranking

| Prioridade | Evidência / problema | Ação / risco / esforço | Como concluir |
|---|---|---|---|
| P0, resolvida | 56 referências de textura sem destino | Recuperação/reuso comprovado; baixo risco estrutural, aparência precisa de comparação | Checagem GUID concluída; comparar capturas em câmera de jogo |
| P1 | Gun.prefab mantém sprite `d73b5e995f0fe33449121aa9eb89aa33` ausente | BLOCKED nesta execução: recurso global também usado pela sala do diretor excluída; não tocar no global. WeaponSpriteAnimator.Start/SetGun atribui idleFrames válidos em runtime; impacto real ainda não testado | Revisar o fallback da UI em tarefa que inclua esse sistema; não substituir fileID por suposição |
| P1 | Aprovação visual e navegação não observadas em jogo | NOT_VERIFIED, todas as cenas; esforço de walkthrough e capturas | Verificar os 12 critérios, sem zero nos critérios essenciais, e mínimo 18/24 |
| P1 | Aviso preexistente de redução de resolução de sombras no atlas 2048² | Não aumentar atlas nem remover sombras sem avaliar VRAM/GPU; risco visual e de memória | Profiler + Frame Debugger e capturas; medir CPU/GPU ms no alvo |
| P2 | Material ProBuilderDefault com shader de erro e cinco GUIDs de pacote sem destino | NOT_APPLICABLE à renderização atual: seis renderizadores do corredor têm enabled=false, no jardim e na cena integrada. Pacote preservado | Só revisar se forem reativados; não modificar PackageCache |
| P2 | MeshFilters de portas sem mesh própria, com renderizadores filhos | NOT_VERIFIED como defeito: podem ser contêineres intencionais; componentes e colisores preservados | Comparar geometria e colisão no Play Mode antes de remover ou preencher |
| P2 | Biblioteca e cena integrada possuem hierarquias grandes | Hipótese de custo de CPU/render submission, sem medição; nenhuma combinação genérica de meshes ou alteração de batching | Medir ms, GC, SetPass, draw calls, memória e culling no build |

## Testes efetivamente executados

- Unity 6000.5.7f1, URP 17.5.0, pipeline PC_RPAsset. Ferramenta temporária compilada e executada no Editor fora de Play Mode; seis cenas abertas em OpenPreviewScene e fechadas sem salvar; cena/seleção de cenas abertas preservada. Resultado final: `completed`, `compilationFailed=false`, sem mensagens capturadas durante a inspeção e sem scripts ausentes. A ferramenta teve duas incompatibilidades de API na primeira compilação, corrigidas antes das execuções; esses erros temporários não são atribuídos ao projeto.
- Inspeção de referências serializadas, materiais/shaders e prefabs expandidos. Não foram detectados erros de shader nos materiais inspecionados; o caso ProBuilder desativado está documentado acima. Não equivale à compilação de todas as variantes de um build.
- Inventário recursivo por GUID e fileID; nenhum GUID duplicado; os 14 GUIDs de textura reparados não permanecem sem destino. Os únicos GUIDs sem resolução são o sprite legado global e cinco referências do material externo ProBuilder.
- Hashes das cenas e da pasta excluída comparados; bytes de imagens/importadores movidos comparados; arquivos recuperados comparados com LFS; diff dos materiais verificado como substituição exclusiva de GUID.
- Busca de consumidores em código: sem caminhos de texto ativos para as oito texturas movidas. Resources.Load de DoorReloadScreen usa arte/áudio de transição em Resources, preservados; não houve alteração de carregamento dinâmico, Addressables, AssetBundles ou lista de build.
- **Não executados:** Play Mode, travessia por portas, encontros/combate, build, dispositivo-alvo, Profiler/Frame Debugger, capturas comparativas, avaliação em janela/tela cheia/monitores ou pontuação visual. Não há ganho de FPS comprovado.

## Retomada, evidência e reversão

`map-manifest.json` contém hashes da cena/regras/dependências/importadores, estados por regra, resultados do Editor e lotes 5+1. `static-audit.json` é o índice/cache; `review_maps.py` verifica novamente hashes e reutiliza parsing compatível. Mudança de dependência relevante invalida a aprovação, que nunca é inferida apenas do cache.

`editor-move-plan.json`, `editor-moves-completed.json`, `reference-recovery.json` e `applied-changes.json` registram origem histórica, SHA-256, destinos e substituições exatas. `baseline-hashes.json` guarda somente hashes, não cópias de assets. `unity-validation.json` registra a última inspeção. `MapReviewEditor.cs` permanece fora de Assets como fonte reproduzível; a cópia temporária compilada é removida ao final.

Para reverter, aplicar inversamente as substituições de `applied-changes.json` e mover as oito texturas de volta pela API da Unity, com os mesmos .meta. Os cinco mapas recuperados só devem ser removidos após reverter os consumidores e verificar dependências; esta revisão não executa exclusões definitivas. Não houve commit automático.

Referências normativas visuais: `.codex/SKILL.md`, seções Princípios, Materiais, Iluminação, Implementação e Checklist. Páginas de referência consultadas: [Hotel Lobby](https://www.moddb.com/mods/total-chaos/images/hotel-lobby) e [Shader implementation](https://www.moddb.com/mods/total-chaos/images/shader-implementation). Nenhum asset do mod foi copiado; essas páginas não substituem avaliação do jogo.
