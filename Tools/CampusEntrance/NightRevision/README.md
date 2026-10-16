# Entrada UniDoom — escala, noite e abandono leve

Revisão solicitada em 16/10/2026, Unity 6000.5.7f1 / URP 17.5.0.

- A arquitetura, portas, luminárias e vegetação recebem altura 30% maior, sem alterar comprimento e largura da circulação. Os bancos também aumentam 20% e os vasos/plantas de interior 12%. O player original permanece com 2,12 m de altura e câmera a 1,91 m do piso.
- Muros perimetrais de 3,25 m e portões externos fechados, com colisores sólidos, ferragens e iluminação sob a cobertura. O jogador começa pelo lado interno do portão e segue até a entrada principal e o corredor.
- Gramado fotográfico PNG 2K ao longo dos dois lados das janelas, com palmeiras ao fundo e preenchimento frio discreto. São elementos de fundo; não foram criadas salas nem rotas extras.
- Ambiente noturno escuro, iluminação localizada nas luminárias, letreiro UniDoom preservado e faixa tátil legível. As configurações locais entram em ação com a câmera do jogador dentro das regiões da entrada; o Scene View pode usar sua própria iluminação de edição.
- Abandono localizado: duas partes ausentes nos cobogós, dois vidros quebrados, lascas em bases de pilares, manchas junto aos muros e pequenos fragmentos diretamente abaixo dos danos. A faixa central de navegação fica livre.

O gramado usa [Grass Ground, Poly Haven](https://polyhaven.com/a/grass_ground), com [licença CC0](https://polyhaven.com/license). `sources.json` registra autores, URLs oficiais e hashes dos PNGs originais. Os mapas existentes de concreto, reboco, vidro e vegetação foram reutilizados. Materiais/texturas compartilhados ficam em `Global/*/CampusNight`; meshes e profiles exclusivos ficam nos cenários correspondentes.

O gramado visto de dentro é um fundo fotográfico com brilho calibrado e shader nativo Unlit, conforme o pedido de PNG, para continuar legível quando a iluminação interior apaga a luz natural. Os modelos de palmeira acrescentam profundidade; o vidro noturno usa uma cópia mais transparente do material original. O diagnóstico confirmou que os revestimentos antigos estão desativados e que a perda de leitura vinha da iluminação/transparência, sem necessidade de alterar a cena principal.

`CampusEntranceAnchor.heightMultiplier` mantém a altura configurada mesmo com os overrides de escala das instâncias. A geometria nova é persistente; o script não gera meshes ou materiais em runtime. A cena principal não precisa ser salva nem substituída por esta revisão.

## Evidências e limites

`validation.txt` registra medidas reais, persistência, referências, colisão do portão e amostragem da circulação. `play-validation.txt` registra o bloqueio do portão e a caminhada com o `CharacterController` original, além das regiões noturnas selecionadas. O EnemySpawner preexistente inválido é desativado somente na cena de teste não salva.

O teste usa deslocamentos curtos e acelerados para amostrar colisões. Para verificar as regiões de atmosfera nesse ritmo, posiciona a Main Camera no alvo `Head`, sem depender do tempo de acompanhamento do Cinemachine. Isso não substitui um teste manual da câmera nem mede velocidade ou desempenho do jogo.

`Exterior-Night.png`, `Closed-Gate.png`, `Corridor-Night.png`, `Window-Lawn.png` e `Facade-Wear.png` são capturas reais da Unity na altura medida da câmera do player, sem edição de imagem. Capturas de diagnóstico não representam o resultado final.

`installed-validation.json` e `installed.json` registram a instalação, GUIDs, fileIDs e hashes; `Before/` guarda os dois prefabs e o script anterior com seus metas, além da cena usada no teste. Não foram executados novo build, profiling de desempenho, bake ou teste manual com teclado/mouse. A amostragem e caminhada automatizadas não medem FPS.

## Reprodução e reversão

Ferramentas pontuais ficam neste diretório e são compiladas apenas na cópia isolada `.utmp/EntranceRebuild/Assets/_TemporaryTools/Editor`. Não execute o gerador anterior `CampusBuilder.Build` para aplicar esta revisão: ele reconstrói a versão anterior.

O fluxo é: preservar o estado atual com `prepare.py`, baixar os mapas com `download.py`, executar `CampusNightRevision.Build`, preservar fileIDs com `install.py --preserve-ids`, executar `CampusNightValidation.Run`, executar `CampusNightPlayCheck.Run` e instalar com `install.py`. `CampusNightRevision.Polish` aplica os ajustes finais de iluminação a uma geração já salva.

Para validar a reversão: `py Tools/CampusEntrance/NightRevision/rollback.py --check`. Com a Unity fechada, `--apply` restaura apenas os prefabs e o script se não houver modificações posteriores. Os assets novos são mantidos; nada é excluído definitivamente.
