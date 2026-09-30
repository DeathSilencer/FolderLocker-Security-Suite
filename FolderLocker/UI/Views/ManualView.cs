using FolderLocker.UI.Common;

namespace FolderLocker.UI.Views
{
    public class ManualView : UserControl
    {
        private Label lblManualTitulo = null!;
        private Label lblManualSubtitulo = null!;
        private Panel card = null!;
        private Panel pnlScroll = null!;

        public Panel CardPanel => card;

        public ManualView()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = UITheme.cBackground;
            InicializarComponentes();
        }

        private void InicializarComponentes()
        {
            lblManualTitulo = new Label
            {
                Text = Localization.Get("manual_title") ?? "GUÍA RÁPIDA DE USUARIO",
                ForeColor = UITheme.cTextPrimary,
                Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold),
                AutoSize = true
            };
            this.Controls.Add(lblManualTitulo);

            lblManualSubtitulo = new Label
            {
                Text = Localization.CurrentLang == "EN"
                    ? "Learn how to encrypt, mount virtual drives, and protect your private data."
                    : "Aprende a cifrar carpetas, montar discos virtuales y proteger tu privacidad.",
                ForeColor = Color.FromArgb(160, 150, 150),
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true
            };
            this.Controls.Add(lblManualSubtitulo);

            // Tarjeta principal (680 x 440)
            card = new Panel
            {
                Size = new Size(680, 440),
                BackColor = UITheme.cSurface
            };
            card.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle, UITheme.cBorder, ButtonBorderStyle.Solid);
                using var b = new SolidBrush(UITheme.cAccentRed);
                e.Graphics.FillRectangle(b, 0, 0, card.Width, 3);
            };
            this.Controls.Add(card);

            // Badges superiores
            int badgeY = 18;
            var badge1 = CrearBadge("📖 GUÍA DE USUARIO", Color.FromArgb(50, 22, 22), Color.FromArgb(252, 165, 165), 40, badgeY);
            var badge2 = CrearBadge("🛡 MEJORES PRÁCTICAS", Color.FromArgb(36, 33, 33), Color.FromArgb(209, 213, 219), 180, badgeY);
            card.Controls.AddRange(new Control[] { badge1, badge2 });

            // Contenedor scrollable moderno
            pnlScroll = new Panel
            {
                Location = new Point(35, 54),
                Size = new Size(610, 368),
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(pnlScroll);

            CargarTarjetasManual();
            Recentrar();
        }

        private void CargarTarjetasManual()
        {
            pnlScroll.SuspendLayout();
            pnlScroll.Controls.Clear();

            int cardWidth = 582;
            int currentY = 0;

            bool isEn = Localization.CurrentLang == "EN";

            // 1. Proteger
            var card1 = CrearGuiaCard(
                "🔒",
                isEn ? "1. HOW TO PROTECT A FOLDER" : "1. CÓMO PROTEGER UNA CARPETA",
                Color.FromArgb(252, 165, 165),
                isEn
                    ? new[]
                    {
                        "• Go to the 'Protect' tab in the Security Center.",
                        "• Select your folder using 'Browse...' or drag and drop it directly onto the window.",
                        "• Enter your master password and click 'Start Protection'. Your files will be secured with military-grade AES-256 CTR and Two-Phase Commit."
                    }
                    : new[]
                    {
                        "• Dirígete a la pestaña 'Proteger' en el Centro de Seguridad.",
                        "• Selecciona tu carpeta con el botón 'Examinar...' o simplemente arrástrala a la ventana.",
                        "• Ingresa tu contraseña maestra y pulsa 'Iniciar Bloqueo'. Tus archivos serán cifrados con AES-256 CTR militar y Two-Phase Commit."
                    },
                Color.FromArgb(30, 27, 27),
                Color.FromArgb(48, 42, 42),
                currentY,
                cardWidth
            );
            pnlScroll.Controls.Add(card1);
            currentY += card1.Height + 12;

            // 2. Abrir Bóveda
            var card2 = CrearGuiaCard(
                "💾",
                isEn ? "2. ACCESS FILES (OPEN VAULT)" : "2. ACCEDER A LOS ARCHIVOS (ABRIR BÓVEDA)",
                Color.FromArgb(56, 189, 248),
                isEn
                    ? new[]
                    {
                        "• Navigate to 'Open Vault' in the left sidebar menu.",
                        "• Select your encrypted folder and pick a virtual drive letter (e.g. M:\\).",
                        "• Enter your master password. The vault will mount as a virtual hard drive in Windows Explorer for seamless real-time access without decrypting."
                    }
                    : new[]
                    {
                        "• Ve a la sección 'Abrir Bóveda' en el menú lateral izquierdo.",
                        "• Selecciona tu carpeta encriptada y escoge una letra de disco virtual (ej. M:\\).",
                        "• Introduce tu contraseña maestra. Tu bóveda aparecerá como un disco duro en Este Equipo para que edites tus archivos en tiempo real sin tener que desencriptar."
                    },
                Color.FromArgb(30, 27, 27),
                Color.FromArgb(48, 42, 42),
                currentY,
                cardWidth
            );
            pnlScroll.Controls.Add(card2);
            currentY += card2.Height + 12;

            // 3. Restaurar
            var card3 = CrearGuiaCard(
                "🔓",
                isEn ? "3. RESTORE (REMOVE PROTECTION)" : "3. RESTAURAR (QUITAR PROTECCIÓN)",
                Color.FromArgb(134, 239, 172),
                isEn
                    ? new[]
                    {
                        "• In the Security Center, select the upper 'Restore' tab.",
                        "• Pick the protected folder from the list and click 'Restore & Decrypt'.",
                        "• The folder will return to normal visible state and all files will be fully restored without modifications."
                    }
                    : new[]
                    {
                        "• En el Centro de Seguridad, pulsa la pestaña superior 'Restaurar'.",
                        "• Selecciona la carpeta protegida de la lista y haz clic en 'Restaurar y Desencriptar'.",
                        "• La carpeta volverá a su estado normal visible y todos tus archivos quedarán completamente desencriptados sin alteraciones."
                    },
                Color.FromArgb(30, 27, 27),
                Color.FromArgb(48, 42, 42),
                currentY,
                cardWidth
            );
            pnlScroll.Controls.Add(card3);
            currentY += card3.Height + 12;

            // 4. Advertencias Críticas
            var card4 = CrearGuiaCard(
                "⚠️",
                isEn ? "4. CRITICAL SECURITY WARNINGS (MUST READ)" : "4. ADVERTENCIAS CRÍTICAS DE SEGURIDAD (LEER OBLIGATORIAMENTE)",
                Color.FromArgb(248, 113, 113),
                isEn
                    ? new[]
                    {
                        "⚠️ NEVER enter the hidden folder to paste or modify files manually! They will be corrupted upon decryption.",
                        "⚠️ Always use the virtual drive (Drive Letter:) to view and edit files transparently.",
                        "⚠️ DO NOT delete 'dir.idx' or 'locker.id' files; they store the encrypted catalog required to access your vault.",
                        "⚠️ Save your REC code safely. If you lose both your password and REC key, recovery is cryptographically impossible.",
                        "⚠️ DO NOT shut down the PC or unplug external storage during encryption or batch decryption operations."
                    }
                    : new[]
                    {
                        "⚠️ ¡NUNCA entres a la carpeta oculta para pegar o borrar archivos manualmente! Se corromperán al intentar desencriptarlos.",
                        "⚠️ Usa siempre la unidad virtual (Letra:) para editar tus archivos de forma segura y transparente.",
                        "⚠️ NO borres los archivos 'dir.idx' ni 'locker.id'; contienen los mapas de cifrado y perderías el acceso a la bóveda.",
                        "⚠️ Guarda tu código REC en un lugar seguro. Si olvidas tu clave y no tienes tu código, no existe forma matemática de recuperar los datos.",
                        "⚠️ NO apagues el equipo ni desconectes el disco mientras se esté cifrando o restaurando una carpeta con muchos archivos."
                    },
                Color.FromArgb(40, 20, 20),
                Color.FromArgb(74, 30, 30),
                currentY,
                cardWidth,
                UITheme.cAccentRed
            );
            pnlScroll.Controls.Add(card4);

            pnlScroll.ResumeLayout(true);
        }

        private Panel CrearGuiaCard(string icon, string titulo, Color colorTitulo, string[] pasos, Color fondo, Color borde, int yPos, int w, Color? acentoIzquierdo = null)
        {
            var pnl = new Panel
            {
                Location = new Point(0, yPos),
                Width = w,
                BackColor = fondo
            };

            pnl.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, borde, ButtonBorderStyle.Solid);
                if (acentoIzquierdo.HasValue)
                {
                    using var b = new SolidBrush(acentoIzquierdo.Value);
                    e.Graphics.FillRectangle(b, 0, 0, 4, pnl.Height);
                }
            };

            var lblHeader = new Label
            {
                Text = $"{icon}  {titulo}",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = colorTitulo,
                Location = new Point(14, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnl.Controls.Add(lblHeader);

            int curY = 34;
            foreach (var paso in pasos)
            {
                var lblPaso = new Label
                {
                    Text = paso,
                    Font = new Font("Segoe UI", 8.8f),
                    ForeColor = Color.FromArgb(220, 210, 210),
                    Location = new Point(18, curY),
                    Width = w - 36,
                    AutoSize = true,
                    MaximumSize = new Size(w - 36, 0),
                    BackColor = Color.Transparent
                };
                pnl.Controls.Add(lblPaso);
                curY += lblPaso.PreferredHeight + 6;
            }

            pnl.Height = curY + 8;
            return pnl;
        }

        private static Label CrearBadge(string text, Color bg, Color fg, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                BackColor = bg,
                ForeColor = fg,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                Padding = new Padding(6, 3, 6, 3),
                AutoSize = true
            };
        }

        public void ActualizarIdioma()
        {
            lblManualTitulo.Text = Localization.Get("manual_title") ?? "GUÍA RÁPIDA DE USUARIO";
            lblManualSubtitulo.Text = Localization.CurrentLang == "EN"
                ? "Learn how to encrypt, mount virtual drives, and protect your private data."
                : "Aprende a cifrar carpetas, montar discos virtuales y proteger tu privacidad.";
            CargarTarjetasManual();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Recentrar();
        }

        private void Recentrar()
        {
            if (card == null) return;
            int totalH = 80 + card.Height;
            int startY = Math.Max(25, (this.ClientSize.Height - totalH) / 2);
            int x = Math.Max(20, (this.ClientSize.Width - card.Width) / 2);

            if (lblManualTitulo != null)
            {
                lblManualTitulo.Location = new Point(x, startY);
            }
            if (lblManualSubtitulo != null)
            {
                lblManualSubtitulo.Location = new Point(x, startY + 34);
            }

            card.Location = new Point(x, startY + 68);
        }
    }
}
