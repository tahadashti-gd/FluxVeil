<img width="2816" height="1536" alt="Gemini_Generated_Image_q3rp65q3rp65q3rp" src="https://github.com/user-attachments/assets/930a8777-a0bf-4a6a-b7b6-825730cf52f4" />

#FluxVeil

A minimal and clean CLI tool written in C\# for securely hiding text and images within other images (Steganography).

### Features

  * **Text Hiding:** Encrypts text with **AES** and hides it within image pixels.
  * **Image Hiding:** Embeds a secret image inside a host image (Stealth Mode).

### Usage

Run the executable to enter interactive mode, or use the following commands:

**1. Hide Text (Encrypted):**

```bash
hide input.png "My Secret Text" output.png MyPassword123
```

**2. Reveal Text:**

```bash
reveal output.png MyPassword123
```

**3. Hide Image inside Image:**

```bash
hide-img host.png secret.png output.png
```

**4. Reveal Image:**

```bash
reveal-img output.png secret_revealed.png
```

-----

Built with .NET 9.0
