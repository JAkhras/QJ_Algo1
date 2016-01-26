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
            this.tbxAll = new System.Windows.Forms.TextBox();
            this.txbAccounts = new System.Windows.Forms.TextBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // tbxAll
            // 
            this.tbxAll.BackColor = System.Drawing.Color.White;
            this.tbxAll.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbxAll.Location = new System.Drawing.Point(20, 62);
            this.tbxAll.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tbxAll.Multiline = true;
            this.tbxAll.Name = "tbxAll";
            this.tbxAll.ReadOnly = true;
            this.tbxAll.Size = new System.Drawing.Size(529, 479);
            this.tbxAll.TabIndex = 46;
            // 
            // txbAccounts
            // 
            this.txbAccounts.BackColor = System.Drawing.Color.White;
            this.txbAccounts.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txbAccounts.Location = new System.Drawing.Point(20, 580);
            this.txbAccounts.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txbAccounts.Multiline = true;
            this.txbAccounts.Name = "txbAccounts";
            this.txbAccounts.ReadOnly = true;
            this.txbAccounts.Size = new System.Drawing.Size(522, 326);
            this.txbAccounts.TabIndex = 48;
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.Color.White;
            this.textBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox1.Location = new System.Drawing.Point(612, 62);
            this.textBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(529, 844);
            this.textBox1.TabIndex = 49;
            // 
            // TestTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1201, 943);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.txbAccounts);
            this.Controls.Add(this.tbxAll);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "TestTool";
            this.Text = "TestTool";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.TextBox tbxAll;
		private System.Windows.Forms.TextBox txbAccounts;
        private System.Windows.Forms.TextBox textBox1;
    }
}