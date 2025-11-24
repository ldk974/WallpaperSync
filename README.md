<p align="center">
  <img src="assets/logo.svg" alt="WallpaperSync logo" width="240"/>
</p>

<p align="center">
  Troca automatizada de papéis de parede
</p>

<p align="center">
  <a href="#recursos">Recursos</a> •
  <a href="#comparação-entre-versões">Comparação entre versões</a> •
  <a href="#downloads">Downloads</a> •
  <a href="#perguntas-frequentes-faq">FAQ</a> •
  <a href="#licença">Licença</a>
</p>

**WallpaperSync** é uma ferramenta que lista imagens hospedadas em um servidor (ou uma imagem fornecida pelo usuário) e aplica a imagem selecionada como papel de parede do Windows.  
Ideal para quem quer trocar rapidamente papéis de parede sem criar vestígios desnecessários.

---

## Principais pontos

- **Rápido** — lista e baixa a imagem escolhida em poucos segundos.  
- **Discreto** — operações locais, sem criar logs persistentes por padrão.  
- **Profissional** — mensagens claras, confirmações e proteções contra erros (rate-limit, downloads inválidos).

---

## Recursos

- Agregação automática de **todas** as imagens presentes no servidor.
- Suporte a imagem customizada fornecida pelo usuário.
- Ordenação contínua por nomes numéricos (ex.: `1.jpg`, `2.jpg`, ...).
- Download resiliente com retries e verificação básica de integridade.
- Substituição do papel de parede do Windows via API com fallback.
- Confirmações antes de qualquer alteração e antes de reiniciar/desligar.

---

## ⚠️ Aviso importante — leia antes de usar

- Este programa **pode alterar arquivos do sistema de forma direta**.  
- As imagens disponibilizadas contém **conteúdo adulto / NSFW.**
- O papel de parede possivelmente só será efetivamente aplicado após **reiniciar o Explorador de Arquivos**. O programa oferece essa opção automaticamente com confirmação.

Se você não concorda: **não execute** o programa.

---

## Requisitos

- Windows 10/11
- PowerShell 5.1 recomendado (para a versão Script PowerShell)
- Acesso à internet (para listar e baixar imagens do servidor)

> Se necessário, você pode permitir execução temporária do script com:
> ```powershell
> Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
> ```

---
## Comparação entre versões

| Feature / Recurso                     | Script PowerShell | App GUI (EXE) |
|---------------------------------------|:-----------------:|:-------------:|
| Aplicar papel de parede               | ✔️                | ✔️            |
| Restaurar papel de parede original    | ❌                | ✔️            |
| Listagem de papeis de paredes         | ✔️                | ✔️            |
| Imagens próprias                      | ⚠️ (apenas url)   | ✔️            |
| Prévias                               | ❌                | ✔️            |
| Fallback automático (TranscodedWallpaper) | ✔️            | ✔️            |
| Interface gráfica                     | ❌                | ✔️            |
| Atualizações futuras                  | ❌ (somente patches) | ✔️         |

---
## Downloads

### **GUI - Windows App**
**[Download V2.0.0 (Gui)](http://github.com/ldk974/wallpapersync/releases/download/gui-v2.0.0/wallpapersync-v2.0.0-bundled.exe)**

### **Script PowerShell**
**[Download V1.0.0 (PowerShell)](http://github.com/ldk974/wallpapersync/releases/download/ps-v1.0.0/wallpapersync.ps1)**

<details open>
<summary>Como utilizar a versão Script PowerShell</summary>

1. Faça o download do `WallpaperSync.ps1` na página de **Releases**.  
2. Clique com o botão direito → **Executar com PowerShell**.  
3. Leia o aviso e confirme.
4. Siga as instruções exibidas no terminal.

</details>

---

## Perguntas frequentes (FAQ)

**P:** WallpaperSync envia dados para fora do meu computador?  
**R:** Não. Não há telemetria.  As versões GUI e Script PowerShell apenas baixam imagens do servidor.

**P:** O WallpaperSync deixa algum rastro?  
**R:** As imagens são baixadas de forma temporária e removidas após realizar o processo.

**P:** Posso desfazer a alteração?  
**R:** Sim — a versão GUI possui uma opção de restaurar o papel de parede original.

**P:** Posso utilizar uma imagem minha?  
**R:** Sim — é possível fornecer uma imagem própria em ambas as versões.

---

## Privacidade & responsabilidade

Você é o único responsável pelo uso deste software e pelo conteúdo aplicado.  
Não utilize em equipamentos de terceiros ou ambientes corporativos sem autorização.  
Ao usar o WallpaperSync, você declara estar ciente dos riscos e concorda em assumir a responsabilidade.

---

## Licença

Este projeto é distribuído sob a licença GPL-3.0.  
Consulte o arquivo LICENSE para detalhes.

---

## Contato / Contribuições

Problemas, dúvidas ou sugestões → abra um **Issue** neste repositório.
