# Cylinder: predominância de luzes fracas e defeituosas

Distribuição das 22 luminárias acesas: **16 fracas e seis normais**. **Dez das fracas piscam**, superando também a quantidade de normais. São seis pendentes e quatro fluorescentes periféricas (setores 3, 20, 27 e 44). As outras seis fracas e as seis normais ficam estáveis.

Continuam normais e estáveis: as três portas principais, a bancada do rádio e as duas fluorescentes que calculam sombras. As dez oscilantes têm sementes/velocidades independentes e apagões breves ocasionais; os respectivos bulbos/tubos acompanham a intensidade. A luz ambiente permanece em RGB 0,60 / 0,65 / 0,68 e as posições, alcance, cor, sombras e pós-processamento não mudam.

São reaproveitados o script compartilhado `TCFlickerLight` e as luminárias atuais, acrescentando quatro componentes a quatro luzes existentes. Nenhum script runtime ou material compartilhado é modificado. Geometria, texturas, colisores, portas e spawnpoints permanecem preservados.

O único asset instalado é `Assets/_Project/Scenes/Cylinder/Prefabs/Cylinder.prefab`, após comparação dos blocos serializados e validação na Unity isolada. Backup em `.utmp/CylinderDistressedLightingBefore`. `revision.py` prepara/instala o candidato e recusa mudanças concorrentes. `prepare_review.py` gera o verificador com as contagens esperadas e exige que todas as dez oscilantes pertençam ao grupo fraco.

Capturas de repouso: `Canteen.png` e `Rear.png`. Os relatórios `edit-validation.txt` e `play-validation.txt` registram compilação/referências, distribuição 16/6, dez oscilações independentes, emissão dos bulbos e estabilidade das outras 12 luzes durante 22 segundos em Play Mode. Build, combate na cena completa e profiling no hardware-alvo não foram executados nesta revisão.

Resultado: aprovado. Todas as dez oscilaram; nove apresentaram apagões na janela observada, e a outra manteve a oscilação suave. As 12 estáveis conservaram suas intensidades exatas. Preservados os 7.436 fileIDs de entrada, com quatro componentes novos e nove blocos existentes alterados. O prefab instalado confere por SHA-256 com a versão testada.

A exceção interna de indexação `UnityEditor.Search.SearchDatabase` já observada nas revisões anteriores dessa cópia voltou a aparecer na inicialização. O teste prosseguiu e concluiu com `CYLINDER_DISTRESSED_LIGHTING_PASS`; essa ocorrência não foi atribuída às luzes.
