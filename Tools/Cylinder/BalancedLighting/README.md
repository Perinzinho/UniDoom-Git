# Cylinder: metade normal, metade fraca

22 luminárias acesas divididas pela intensidade base: **11 normais e 11 fracas**. O número de luzes piscando dobrou de três para **seis**, distribuídas igualmente: três normais e três fracas. Piscar é um comportamento dentro desses grupos, sem alterar a proporção de intensidade base.

As seis piscantes são três pendentes e três fluorescentes periféricas (setores 3, 27 e 44). Cada uma usa uma semente de ruído própria, velocidades diferentes e apagões breves ocasionais. Os tubos/bulbos acompanham a intensidade da sua própria luminária. As duas fluorescentes com sombras e a iluminação das três portas principais continuam estáveis.

O script `TCFlickerLight` compartilhado não foi alterado. Dois componentes existentes adicionais foram ativados e apenas um foi acrescentado a uma luminária existente. As posições, alcances, cores, sombras, ambiente RGB 0,60 / 0,65 / 0,68, materiais, geometria, colisores, portas e spawnpoints permanecem preservados.

Somente `Assets/_Project/Scenes/Cylinder/Prefabs/Cylinder.prefab` é instalado. Backup de entrada: `.utmp/CylinderBalancedLightingBefore`. `revision.py` reaproveita as verificações de preservação e concorrência da revisão anterior; `prepare_review.py` adapta o verificador para exigir 11/11, seis componentes ativos e três piscantes em cada faixa de intensidade.

Capturas em `Canteen.png` e `Rear.png`. Compilação/referências e contagens verificadas na Unity isolada; a validação temporal em `play-validation.txt` observa 22 segundos de Play Mode, verifica as seis oscilações, os apagões, a emissão dos bulbos e as 16 luzes estáveis. Build, combate na cena completa e profiling no hardware-alvo não fazem parte desta revisão.

Resultado: testes aprovados. As seis luminárias oscilaram independentemente; quatro tiveram apagões durante a janela observada e as outras duas mantiveram oscilação suave. As 16 estáveis conservaram as intensidades exatas. Preservados 7.435 fileIDs, com um componente novo e nove blocos existentes alterados. O prefab instalado coincide por SHA-256 com a versão validada.

A indexação interna `UnityEditor.Search.SearchDatabase` voltou a registrar a exceção já observada na cópia isolada anterior. O teste concluiu normalmente com `CYLINDER_BALANCED_LIGHTING_PASS`; essa ocorrência não foi atribuída à alteração das luzes.
