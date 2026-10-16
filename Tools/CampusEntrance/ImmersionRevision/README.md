# Entrada UniDoom — continuidade visual e iluminação

Revisão de 16/10/2026, sobre os prefabs já ajustados pela revisão `NightRevision`.

As duas laterais envidraçadas do corredor agora têm pátios cenográficos com gramado existente, palmeiras, samambaias, muros de reboco envelhecido, pilares, capeamentos e postes. Os muros têm retornos nas extremidades. Uma segunda envoltória opaca, com base, laterais, extremidades e céu texturizado, cobre também as visadas acima dos muros e abaixo do gramado. A textura do céu é reutilizada do acervo da biblioteca, sem novo download.

Essa envoltória é geometria persistente no prefab do corredor. A cobertura visual independe da névoa, da iluminação ambiente e do vidro, inclusive nos dois vãos danificados. Os pátios são cenário de fundo, sem novas salas ou caminhos. A abertura principal e a conexão final continuam livres. Os novos objetos não possuem colisores de gameplay; a validação acrescenta colisores temporários apenas para testar as visadas, sem salvar a cena.

Na área inicial, os seis postes passam de intensidade 4,2 para 10 e de alcance 8,5 para 10,5 m. As duas luzes sob a cobertura do portão passam de 5,4 para 8,2, e o ambiente exterior recebe um pequeno aumento de preenchimento frio. O perfil noturno e as luzes do corredor permanecem preservados.

## Verificação

- Compilação e renderização na Unity 6000.5.7f1 / URP 17.5.0, em cópia isolada, com Direct3D 11.
- 1.749.600 raios através das 24 janelas: três posições horizontais e três alturas por vão; elevação e azimute de -89° a +89°. Todos devem atingir a envoltória opaca antes de 125 m.
- Capturas nas duas laterais, no começo, meio e fim, incluindo vistas inclinadas para cima/baixo, na altura medida do player (1,91 m).
- 326 amostras de circulação e apoio do piso, checagem de materiais, scripts, meshes persistentes, muros, portão fechado e iluminação dos postes.
- Teste automatizado em Play Mode com o CharacterController original do jogo, incluindo bloqueio pelo portão e caminhada até o fim do corredor. O EnemySpawner preexistente com lista vazia é desativado somente na cena de teste não salva. O teste acelerado posiciona a Main Camera no alvo Head para verificar a seleção das regiões sem atraso do Cinemachine.

Os resultados efetivos estão em `validation.txt`, `play-validation.txt`, capturas PNG, logs e `installed-validation.json`. Não foram executados novo build, profiling no dispositivo-alvo, bake nem teste manual com teclado/mouse. Os raios cobrem as aberturas envidraçadas; não certificam outras áreas preexistentes do jogo, câmeras fora do corredor ou alterações posteriores da geometria.

## Reprodução e preservação

`prepare.py` preserva o baseline atual em `Before/` e sincroniza os assets com `.utmp/EntranceRebuild`. As ferramentas C# são temporárias e compiladas somente em `Assets/_TemporaryTools/Editor` dessa cópia isolada. O gerador `CampusImmersionRevision.Build` requer o baseline sem esta revisão e recusa reaplicar sobre o grupo já criado.

Depois da geração: `py Tools/CampusEntrance/ImmersionRevision/install.py --preserve-ids`, executar `CampusImmersionValidation.RunAndPlay` sem `-quit` e instalar com `install.py`. O instalador confere os hashes atuais contra o baseline, preserva GUIDs/fileIDs e instala somente os dois prefabs e os novos assets referenciados. A cena principal não é copiada nem salva. As cópias anteriores e seus metas ficam preservadas; nada é apagado.
