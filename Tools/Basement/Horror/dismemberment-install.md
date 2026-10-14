# Instalação dos corpos mutilados

Atualizado em 14/10/2026 usando o Unity Editor aberto no projeto principal. A
fonte é a malha anatômica da maca; os dois torsos não têm cabeça e cada um
perdeu um braço e uma perna desde a coxa. As partes destacadas ficam próximas,
com superfícies de corte e sangue no piso.

O prefab salvo contém os dois corpos sob `Arena horror dressing`, consolidados
em três malhas estáticas por material e sem colliders. A conferência estrutural
reteve os 1.850 IDs de Basement e os 125 IDs anteriores de BasementFinalArena;
os 310 e 24 blocos de colisão/gameplay dos respectivos snapshots permaneceram
idênticos.

Em 14/10/2026, todo o conteúdo de `BasementFinalArena.prefab` foi incorporado
diretamente a `Assets/prefabs/Scenario/Basement.prefab`. A instância aninhada foi
desempacotada; a decoração, a estação, os corpos e as luzes piscantes estão na
hierarquia unificada, sem uma segunda cópia dos restos. O Editor confirmou a
arena desempacotada, os três renderizadores dos restos, as luzes piscantes e a
mesma contagem de colliders antes e depois. Como parte do desempacotamento, o
Unity recriou alguns IDs internos dos objetos da arena.

`dismemberment-metrics.json` registra as contagens de triângulos. O verificador
do editor confirmou os dois grupos, os três renderizadores e a ausência de
colliders. O teste de Play Mode e novas capturas não foram executados após a
unificação completa.
