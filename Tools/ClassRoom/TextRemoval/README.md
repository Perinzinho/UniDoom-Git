# ClassRoom sem textos — 15/10/2026

Removidos os oito textos visíveis da hierarquia de
`Assets/prefabs/Scenario/ClassRoom.prefab`, no nested prefab
`Assets/Art/ClassRoom/AbandonedClassRoomInterior.prefab`: SAÍDA, EVACUAÇÃO,
lema da faculdade, aula/fórmulas, NÃO ABRAM A PORTA, ISOLAMENTO,
aviso de fechamento do campus e ELES AINDA ESTÃO AQUI.

O conteúdo dos oito TextMesh foi esvaziado e seus oito MeshRenderers foram
desativados. Objetos, componentes, fileIDs, placas, suportes, materiais, luzes,
colliders e scripts de gameplay foram preservados. As texturas efetivamente
usadas nos papéis, mapa e quadro não contêm inscrições legíveis que exigissem
edição de imagem. Os assets antigos de folhas com exercícios não são usados
nesses materiais e foram mantidos.

Validação executada na Unity 6000.5.7f1 isolada: nenhuma string de texto restante
nos componentes TextMesh, TMP ou UI da instância completa, incluindo objetos
inativos; renderizadores dos TextMesh desativados. Oito capturas em 1280 × 720
cobrem quatro vistas nos pipelines PC/Mobile, ambos no computador. Nenhum erro
de shader nas variantes renderizadas. [Captura final](preview.png).

A integração alterou somente o campo de conteúdo dos oito TextMesh e a flag
enabled de seus renderizadores, preservando os 2.986 fileIDs do interior.
O verificador da otimização foi atualizado para reconhecer essas alterações
solicitadas e continua validando os reparos de textura e meshes.

O build Windows da etapa anterior antecede esta remoção; não foi repetido.
Não houve nova medição de desempenho ou teste de gameplay.
Logs, backup anterior e capturas completas: `.utmp/ClassRoomTextRemoval`.

Para reverter somente a remoção de textos, conferir
`git apply --check -R Tools/ClassRoom/TextRemoval/changes.patch` e, se passar,
executar `git apply -R Tools/ClassRoom/TextRemoval/changes.patch`.
O patch agregado em `Tools/ClassRoom/Optimization/changes.patch` também foi
atualizado e reverte otimização, reparo de texturas e remoção de textos juntos.
Não aplicar ambos os patches de reversão na mesma sequência.

O utilitário `ClassRoomTextRemoval.cs` pode ser copiado para Assets/Editor de
uma cópia isolada e executado pelo método `ClassRoomTextRemoval.Run`.
