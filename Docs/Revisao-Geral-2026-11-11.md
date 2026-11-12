# Revisão geral do UniDoom — 11/11/2026

Este arquivo reúne **erros possíveis, pontos de validação, melhorias e ideias** encontrados nesta revisão. A fonte técnica foi o checkout atual, o inventário de placas da revisão anterior, a compilação/importação no Unity 6000.5.7f1 e a checagem de colisores do restaurante ampliado. Não é uma afirmação de que todos os sistemas do jogo foram testados em Play Mode.

## Achados confirmados

| ID | Prioridade | Evidência | Ação sugerida |
| --- | --- | --- | --- |
| R-01 | Média | O colisor de `Utility lamp 11`, centrado em `(7; 2,56; 80)`, cruza o volume do restaurante em `(-0,9; 0; 79)`. A checagem após a ampliação encontrou **um** colisor externo sobreposto; a palmeira oeste deixou de sobrepor. Não há captura que prove clipping visível. | Conferir em perspectiva e, se o poste atravessar piso/cobertura ou bloquear passagem, reposicioná-lo junto à borda sem alterar a posição do restaurante. |
| R-02 | Baixa | `Docs/Performance-Optimization.md` ainda afirma que `DoorReloadScreen` chama `Shader.WarmupAllShaders`; o código atual não contém essa chamada. `Docs/Maps/Basement-Celas-Pendencias.md` ainda descreve o gerador como implementação futura no resumo histórico, embora `LibraryMissionProgress` e `PrisonProgress` já usem o estado de energia. | Corrigir esses trechos históricos com uma nota explícita de estado atual, para não orientar uma mudança equivocada. |
| R-03 | Baixa | `UniversityFoodCourt.prefab` permanece no projeto. Seu GUID `9908918a9d0e4797979cd979fd163d03` não apareceu em cenas, prefabs ou ScriptableObjects rastreados; o restaurante atual usa `UniversityRestaurant.prefab`. Isso **não prova** ausência de carregamento dinâmico. | Antes de qualquer descarte, verificar consumidores em código, Resources, Addressables, AssetBundles e dependências por GUID. Preservar o asset até concluir essa análise. |

## Riscos e testes ainda necessários

| ID | Prioridade | Situação | Verificação proposta |
| --- | --- | --- | --- |
| V-01 | Alta | A ampliação do restaurante foi compilada e aberta no Editor, mas não houve Play Mode nem captura em perspectiva. A área atual é **16,48 × 11,5 m**, com centro da instância preservado em `(-0,9; 0; 79)`. | Caminhar pela frente, laterais e balcão; conferir contato de piso, mesas, pilares, plantas, cobertura e letreiros, com atenção ao poste R-01. |
| V-02 | Alta | A cadeia **gerador da biblioteca → 14 celas → retorno ao campus → final** tem verificações isoladas documentadas em `Docs/Maps/Basement-Celas-Pendencias.md`, mas o percurso completo em jogo continua pendente. | Executar uma sessão real antes e depois de ligar o gerador, abrir as 14 celas, atravessar o portal e conferir créditos, controles e checkpoints. |
| V-03 | Média | A padronização das placas foi compilada e as 11 cenas foram carregadas na revisão anterior, mas não houve leitura visual na câmera do jogador. | Conferir contraste e legibilidade das placas em luz normal/apagão, sobretudo sala 423, manutenção, biblioteca e saídas. Preservar pistas e avisos de acesso. |
| V-04 | Média | O Build Settings contém **11 cenas habilitadas** e os 11 caminhos/`.meta` existem. Não foi executado um build após as alterações atuais. | Gerar um build de desenvolvimento e percorrer as transições, especialmente `CampusExterior` e `CampusLibrary`. |
| V-05 | Média | A checagem espacial do restaurante comparou o volume do piso com colisores externos menores que 100 m em X/Z; ela não mede malhas visuais sem colisor, sombras nem colisores extensos de terreno/rua. | Fazer uma inspeção visual e, se houver conflito, comparar também `Renderer.bounds` dos vizinhos. |

## Melhorias técnicas possíveis

| ID | Proposta | Condição antes de implementar |
| --- | --- | --- |
| M-01 | O prefab do restaurante contém **59 `MeshCollider`** em peças estáticas. Estudar colisores simples para cadeiras, mesas e equipamentos, mantendo o piso e a passagem fiéis à geometria. | Medir CPU/Physics e testar contato, tiros e navegação; a contagem isolada não demonstra gargalo. |
| M-02 | O método `BuildUniversityRestaurant.Build()` ainda é uma reconstrução completa do prefab e da instância. O novo `EnlargeExisting()` altera dimensões no asset persistente e preserva a transformação da cena. | Preferir edições incrementais; se houver nova reconstrução completa, comparar fileIDs, GUIDs, componentes e overrides antes/depois. |
| M-03 | Os mapas grandes têm muitos renderers/luzes segundo `Docs/Performance-Optimization.md`, mas não existe baseline atual de GPU/CPU para a versão de hoje. | Capturar Profiler e Frame Debugger em build, no mesmo hardware e enquadramento, antes de reduzir luzes, sombras ou LODs. |

## Ideias de implementação

| ID | Ideia | Papel no jogo |
| --- | --- | --- |
| I-01 | Dar ao restaurante uma pequena ação opcional: encontrar um interruptor de serviço para acender apenas o balcão e revelar suprimentos ou uma pista. | Recompensa exploração sem criar nova chave obrigatória nem alterar os portais. |
| I-02 | Usar cardápios e quadro de avisos para contar uma rotina interrompida — preços parcialmente apagados, data e recado de fechamento — com uma única pista útil. | Fortalece identidade do restaurante e evita excesso de placas explicativas. |
| I-03 | Criar uma passagem lateral clara entre o restaurante e a rota principal do campus, se o teste de percurso mostrar que a grama não conduz o jogador naturalmente até ele. | Melhora descoberta do espaço sem acrescentar texto de tutorial. |

**Sem erro de compilação observado** na última execução do Unity. `git diff --check` passou para as alterações do restaurante. Os itens `V-*` e `M-*` são hipóteses ou verificações pendentes, não defeitos confirmados.
