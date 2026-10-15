# Cylinder: variação das luminárias existentes

Ajuste sobre a revisão `InteriorLighting`, preservando as posições e a base ambiente clara aprovada pelo usuário.

- 14 luzes normais e estáveis, incluindo as três portas principais, a bancada do rádio e as duas fluorescentes com sombras.
- Cinco luzes fracas: um pendente (60 → 26), duas fluorescentes periféricas (65 → 28 / 38), um balcão da cantina (7 → 3) e o vestígio da galeria (7 → 3,5).
- Três luminárias oscilantes: um pendente e as fluorescentes dos setores 3 e 27. Oscilações suaves de 18–25%, velocidades diferentes e apagões curtos ocasionais. Probabilidades por segundo: 0,10 / 0,14 / 0,08. A duração dos apagões de 0,05–0,35 s vem do comportamento existente.

O script compartilhado `TCFlickerLight` é reutilizado sem mudanças. Um componente existente foi ativado e dois foram adicionados a luzes existentes. Cada lâmpada usa seu ruído/semente próprios; os tubos/bulbos do respectivo equipamento acompanham a intensidade com `MaterialPropertyBlock`, sem modificar materiais compartilhados. As luzes que piscam não calculam sombras.

Somente `Assets/_Project/Scenes/Cylinder/Prefabs/Cylinder.prefab` é instalado. Os transforms, os GUIDs/fileIDs existentes, as cores, o alcance e a distribuição geral das luzes, a luz ambiente RGB 0,60/0,65/0,68, o pós-processamento, a geometria e os sistemas de gameplay permanecem preservados.

`revision.py prepare` salva o estado de entrada em `.utmp/CylinderAbandonedLightingBefore`; `candidate` prepara a revisão na cópia isolada e compara os blocos serializados. `CylinderAbandonedLightingReview.Run` verifica referências, renderiza duas vistas e observa a oscilação em Play Mode. `revision.py install` exige os relatórios de aprovação, confere o SHA-256 validado e recusa alterações concorrentes.

As imagens `Canteen.png` e `Rear.png` mostram a distribuição de intensidades em repouso. A variação temporal é registrada em `play-validation.txt`, com mínimos, máximos, apagões observados e verificações da emissão. Não foram executados combate no mapa completo, build ou profiling no hardware-alvo nesta revisão.

Validação concluída: referências e compilação aprovadas; 22 segundos de Play Mode isolado, com dois apagões observados em cada luminária e emissão acompanhando a intensidade. As 19 luzes estáveis mantiveram suas intensidades exatas. Os 7.433 fileIDs anteriores foram preservados e dois componentes foram acrescentados; apenas oito blocos existentes mudaram. O prefab instalado coincide por SHA-256 com o candidato testado.

O início do Play Mode registrou novamente a exceção interna de indexação `UnityEditor.Search.SearchDatabase`, já observada nessa cópia isolada nas revisões anteriores. Ela não impediu o teste, que concluiu com `CYLINDER_ABANDONED_LIGHTING_PASS`; não foi atribuída à alteração das luminárias.
