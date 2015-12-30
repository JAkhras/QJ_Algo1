namespace QJExternalTool
{
	partial class TestTool
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
		    if (disposing && (components != null))
		        components.Dispose();
		    base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            tbxAll = new System.Windows.Forms.TextBox();
            txbAccounts = new System.Windows.Forms.TextBox();
            SuspendLayout();
            // 
            // tbxAll
            // 
            tbxAll.BackColor = System.Drawing.Color.White;
            tbxAll.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            tbxAll.Location = new System.Drawing.Point(20, 61);
            tbxAll.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tbxAll.Multiline = true;
            tbxAll.Name = "tbxAll";
            tbxAll.ReadOnly = true;
            tbxAll.Size = new System.Drawing.Size(529, 558);
            tbxAll.TabIndex = 46;
            // 
            // txbAccounts
            // 
            txbAccounts.BackColor = System.Drawing.Color.White;
            txbAccounts.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            txbAccounts.Location = new System.Drawing.Point(20, 651);
            txbAccounts.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            txbAccounts.Multiline = true;
            txbAccounts.Name = "txbAccounts";
            txbAccounts.ReadOnly = true;
            txbAccounts.Size = new System.Drawing.Size(522, 86);
            txbAccounts.TabIndex = 48;
            // 
            // TestTool
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(562, 822);
            Controls.Add(txbAccounts);
            Controls.Add(tbxAll);
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            Name = "TestTool";
            Text = "TestTool";
            ResumeLayout(false);
            PerformLayout();

		}

		#endregion
		private System.Windows.Forms.TextBox tbxAll;
		private System.Windows.Forms.TextBox txbAccounts;
    }
}