# Basement: selos e corredor

Direção aplicada: `total-chaos-visual-direction`, com referência ao [Total Chaos clássico no ModDB](https://www.moddb.com/mods/total-chaos/images/hotel-lobby) e às capturas de manutenção já guardadas em `.utmp/TotalChaosReferences`. A interpretação prioriza função industrial, ferrugem localizada, umidade seguindo juntas, iluminação existente e uma rota legível. Nenhum asset do mod foi incorporado.

## Alterações

- **Selos nos pisos:** os dois selos usam um pentagrama com uma única faixa circular de sigilos, reunindo as três frases solicitadas. O desenho da pré-arena mantém 6,5 m de diâmetro e é compartilhado pelo `Basement.prefab` e pelo `BasementFinalArena.prefab`. Pigmento vermelho terroso e giz, sem emissão. A etapa atual, o alfabeto e as capturas estão documentados em [Sigils/README.md](Sigils/README.md).
- **Corredores X e Z:** círculos e glifos do piso e das paredes foram ocultados. Entraram duas linhas de serviço com braçadeiras, válvula, eletrocalha, cabo inferior, tampa de manutenção, escorrimentos junto à parede e manchas irregulares no rodapé. O centro da passagem e o vão da curva permanecem livres.
- **Escala na cena:** os pisos têm dimensões diferentes no prefab e em `SampleScene`. As instalações recebem malhas ajustadas para cada comprimento e overrides apenas nos objetos visuais novos. Assim, a decoração mantém escala mundial 1 nos dois contextos.
- **Textos removidos:** por solicitação final, as seis placas com lettering foram retiradas, incluindo o aviso herdado do sanctum. Os componentes TextMesh antigos ficam vazios e com renderers desativados, inclusive na arena independente. O gerador foi atualizado para manter o Basement sem textos.

Não foram acrescentados scripts de runtime, luzes, sombras ou colliders. Os materiais de manutenção derivam dos PBR existentes. As malhas e materiais originais continuam disponíveis. As otimizações anteriores do shader e das referências de mesh foram mantidas.

## Arquivos

- `Assets/Art/Basement/VisualRepair`: novas malhas, materiais e texturas.
- `Assets/prefabs/Scenario/Basement.prefab`: decoração visual, sem textos ou placas com lettering.
- `Assets/Art/Basement/BasementFinalArena.prefab`: mesmo selo corrigido.
- `Assets/Art/Basement/BasementFinalSanctum.prefab`: remoção do letreiro herdado.
- `Assets/Scenes/SampleScene.unity`: somente overrides de escala/mesh de objetos visuais novos da instância Basement.
- `BasementVisualRepair.cs`, `prepare.py`, `install.py`: geração isolada, snapshot e mesclagem controlada. Os scripts C# de geração ficam em Tools, fora da compilação do jogo.

## Validação

Unity **6000.5.7f1 / URP 17.5.0**. Capturas antes/depois com as mesmas câmeras, atmosfera regional e lanterna, incluindo vista elevada do selo e altura do jogador. Uma sessão nova da Unity carrega as malhas salvas antes da revisão final.

- Renderização e referências de meshes/materiais verificadas na Unity.
- Travessia com o **CharacterController real** no mapa e no prefab; portão fechado bloqueia e aberto libera a passagem.
- Simulação do encontro: 12 inimigos, último sobrevivente mantém o portão fechado, última morte libera, boss excluído.
- Play Mode: atmosfera regional, lanterna, volumes, luzes oscilantes e restauração do ambiente externo.
- Todos os fileIDs antigos preservados. Campos originais de física, scripts, luzes, volumes e LOD não foram alterados. O instalador preserva os overrides anteriores da cena.
- Cada corredor usa quatro batches de material para a instalação nova, sem novos colliders. Contagens de triângulos e resultados em [validation.txt](validation.txt) e [runtime-validation.txt](runtime-validation.txt).

Não foi gerado outro Player build para esta revisão de arte e não houve medição de FPS. A avaliação de desempenho permanece pendente no hardware-alvo. Os logs isolados também contêm avisos preexistentes de pacotes e de resolução do atlas de sombras; eles não são ganhos nem regressões medidos desta alteração.

## Comparação e reversão

[Comparação do selo e dos corredores](comparison.png). As capturas completas `BeforeRepair-*` e `VerifiedRepair-*` estão nesta pasta.

A remoção final dos textos está registrada em `text-removal.json` e `text-removal-validation.txt`: seis placas retiradas, textos antigos vazios, verificação no mapa e no prefab e testes de travessia/portão aprovados. O snapshot imediatamente anterior a essa remoção fica em `.utmp/BasementTextRemoval/before` (três prefabs).

O snapshot desta etapa fica em `.utmp/BasementVisualRepair/before`, já incluindo a otimização anterior. Para reverter somente esta revisão, restaure dali os três prefabs e `SampleScene.unity`, desde que não existam edições posteriores nesses arquivos. Os assets novos podem permanecer sem referências. `staging-report.json` registra exatamente quais campos visuais foram mesclados.

Revisão pela checklist da skill: escala e encaixes conferidos na cena e no prefab; materiais com respostas distintas; desgaste localizado; iluminação motivada pelas luminárias e velas existentes; passagem e alvo ao fundo legíveis. Nenhuma luz nova foi usada para encobrir defeitos. O desempenho no alvo ainda não recebeu aprovação por medição.
