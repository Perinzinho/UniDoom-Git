# Cylinder: interior mais claro

Ajuste solicitado exclusivamente na iluminação, usando a temperatura fria das fluorescentes e os acentos quentes da sala de aula como referência.

O único asset alterado é `Assets/_Project/Scenes/Cylinder/Prefabs/Cylinder.prefab`. A ferramenta preserva todos os fileIDs, GUID, hierarquia, modelos, materiais, texturas, colisores, portas e spawnpoints. O pós-processamento permanece igual.

| Fonte existente | Antes | Depois |
|---|---|---|
| Ambiente regional RGB | 0,153 / 0,1785 / 0,204 | 0,60 / 0,65 / 0,68 |
| Seis pendentes: intensidade / alcance | 18 / 15 m | 60 / 20 m |
| Seis fluorescentes periféricas: intensidade / alcance | 10 / 10 m | 65 / 16 m |
| Nove luzes quentes: intensidade / alcance | 3,4 / 7 m | 7 / 9 m |
| Luminária do rádio: intensidade / alcance | 4 / 3,8 m | 6 / 4,5 m |

Nenhuma luz ou sombra nova. Luzes anteriormente apagadas continuam no mesmo estado. A iluminação ambiente maior permite ver móveis, paredes e circulação mesmo sem a lanterna.

Os seis pendentes passam de Spot a Point para iluminar também superfícies verticais e teto. As seis fluorescentes periféricas mantêm Spot, com cones de 145° e núcleo de 95°, distribuindo a luz por uma área maior. Os modos de sombra existentes permanecem iguais; nenhum Point novo passa a calcular sombras.

As capturas `Before-*.png` / `After-*.png` usam câmeras idênticas, o mesmo pós-processamento e lanterna desativada. A validação na Unity isolada verifica carregamento e referências do prefab final. `changes.json` documenta a comparação de todos os blocos: somente tipo, intensidade, alcance e abertura das luzes existentes, além de `TCRegionAtmosphere.ambient`, podem mudar.

Backup em `.utmp/CylinderInteriorLightingBefore`. `revision.py prepare` preserva o prefab de entrada e sincroniza a cópia isolada. `CylinderInteriorLightingReview.Run` captura antes, prepara o candidato e captura depois, sem salvar ou regenerar geometria pelo Editor. `revision.py install` instala apenas o prefab após conferir o SHA-256 validado e recusar alterações concorrentes.

Não foram executados Play Mode da cena completa, build ou profiling de desempenho nesta revisão de iluminação.
