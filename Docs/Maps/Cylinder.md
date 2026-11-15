# Redondo — Cylinder

## Leitura rápida

- Cena: `Assets/_Project/Scenes/Cylinder/Cylinder.unity`; cadeia jogável: `Cylinder.unity` → `Prefabs/CylinderScenario.prefab` → `Prefabs/Cylinder.prefab`. A variante não sobrescreve o estado ativo dos objetos afetados.
- **Símbolos e iluminação das portas — 15/11/2026:** dos 46 grupos `Door identity` nos vãos 2–47, somente os vãos 12 (sala de aula), 24 (diretoria) e 36 (biblioteca) permanecem visíveis. Os outros 43, pertencentes a portas seladas, foram desativados sem excluir objetos ou mudar colisões. As quatro luzes locais dos vãos selados 6, 18, 30 e 42 também foram desativadas, incluindo seus difusores visuais. As luzes dos vãos acessíveis 12, 24 e 36 permanecem ligadas. A entrada frontal e a iluminação geral do átrio foram preservadas. Conferência estática e `git diff --check` passaram; Unity, Play Mode e comparação visual não foram executados por falta de licença válida no Editor em lote.
- **Portas seladas sem aviso — 15/11/2026:** o contato com uma `LockedDoorSurface` do Redondo não exibe mais “Porta trancada.”. O retorno sonoro existente continua; avisos de portas interativas que exigem chave não mudam. Conferência estática e `git diff --check`; Play Mode pendente.
