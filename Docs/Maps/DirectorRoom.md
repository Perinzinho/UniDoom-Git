# Diretoria — DirectorRoom

## Leitura rápida

- Cena: `Assets/_Project/Scenes/DirectorRoom/DirectorRoom.unity`; arquitetura e mesa: `Prefabs/DirectorRoom.prefab`. A instância da arquitetura na cena sobrescreve posição e rotação da raiz, preservando a escala `(1,74; 1,3; 1,54)` do prefab.
- **AK-47 sobre a mesa — 16/11/2026:** a instância já existente de `Global/Prefabs/Weapons/AK47Pickup.prefab` foi movida da antiga posição `(-0,2; 0,68; 2,4)` para `(-20,35; 2,98; 14,45)`, na metade frontal e direita do tampo. Escala visual `(0,6; 0,6; 1)`. A altura foi calculada a partir do collider da mesa (tampo em aproximadamente 2,69 m), da escala da arquitetura e do sprite de 154 × 96 px a 100 pixels por unidade. A arma permanece como coleta interativa; GUIDs, prefab, definição e script de coleta não foram alterados. Conferência estática concluída; visual na Unity, alcance em Play Mode e build pendentes.
- **Correção de visibilidade da AK-47 — 16/11/2026:** a instância da cena e os GUIDs estavam presentes, mas o pickup e os quadros da definição usavam `fileID`s de sprites ausentes do importador atual de `AK47_sprt.png`. As referências foram religadas aos 26 sprites existentes, na ordem de idle e disparo. Cena e posição não mudaram. Conferência estática das referências concluída; visual na Unity e coleta em Play Mode pendentes.
