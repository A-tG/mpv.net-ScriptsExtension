[mpv.net](https://github.com/stax76/mpv.net) Extension to keep all my scripts in one project (also it easier to work with extension than csharp script - debugging, compiling, unsafe code, etc)

[Download](https://github.com/A-tG/mpv.net-ScriptsExtension/releases/latest/download/AtgScriptsExtension.zip) and unpack dll to `%APPDATA%\mpv.net\extensions\AtgScriptsExtension\`

# Scripts
 Add to `input.conf`
 
### Copy screenshot directly into clipboard.
`Ctrl+c script-message atg_screenshot-to-clipboard [flags]`

 [flags](https://mpv.io/manual/master/#command-interface-screenshot-%3Cflags%3E) from screenshot command
 
 ### Pause/unpause + rewind if video is ended
 `Space script-message atg_cycle-pause`
