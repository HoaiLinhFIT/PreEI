namespace PreEIApplication
{
    partial class PreEIForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.resultsTreeView = new System.Windows.Forms.TreeView();
            this.lowerThresholdTextBox = new System.Windows.Forms.TextBox();
            this.lowerThresholdLabel = new System.Windows.Forms.Label();
            this.runMiningButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.saveResultsButton = new System.Windows.Forms.Button();
            this.upperThresholdLabel = new System.Windows.Forms.Label();
            this.upperThresholdTextBox = new System.Windows.Forms.TextBox();
            this.browseDatasetButton = new System.Windows.Forms.Button();
            this.datasetPartsListBox = new System.Windows.Forms.ListBox();
            this.splitConfigurationLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // resultsTreeView
            // 
            this.resultsTreeView.ItemHeight = 30;
            this.resultsTreeView.Location = new System.Drawing.Point(50, 331);
            this.resultsTreeView.Margin = new System.Windows.Forms.Padding(6);
            this.resultsTreeView.Name = "resultsTreeView";
            this.resultsTreeView.ShowLines = false;
            this.resultsTreeView.Size = new System.Drawing.Size(1116, 889);
            this.resultsTreeView.TabIndex = 0;
            // 
            // lowerThresholdTextBox
            // 
            this.lowerThresholdTextBox.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lowerThresholdTextBox.ForeColor = System.Drawing.SystemColors.Highlight;
            this.lowerThresholdTextBox.Location = new System.Drawing.Point(214, 41);
            this.lowerThresholdTextBox.Margin = new System.Windows.Forms.Padding(6);
            this.lowerThresholdTextBox.Name = "lowerThresholdTextBox";
            this.lowerThresholdTextBox.Size = new System.Drawing.Size(309, 50);
            this.lowerThresholdTextBox.TabIndex = 3;
            this.lowerThresholdTextBox.Text = "1.05";
            // 
            // lowerThresholdLabel
            // 
            this.lowerThresholdLabel.AutoSize = true;
            this.lowerThresholdLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lowerThresholdLabel.ForeColor = System.Drawing.SystemColors.Highlight;
            this.lowerThresholdLabel.Location = new System.Drawing.Point(41, 47);
            this.lowerThresholdLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lowerThresholdLabel.Name = "lowerThresholdLabel";
            this.lowerThresholdLabel.Size = new System.Drawing.Size(174, 45);
            this.lowerThresholdLabel.TabIndex = 4;
            this.lowerThresholdLabel.Text = "Lower (%)";
            // 
            // runMiningButton
            // 
            this.runMiningButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.runMiningButton.ForeColor = System.Drawing.SystemColors.Highlight;
            this.runMiningButton.Location = new System.Drawing.Point(604, 258);
            this.runMiningButton.Margin = new System.Windows.Forms.Padding(6);
            this.runMiningButton.Name = "runMiningButton";
            this.runMiningButton.Size = new System.Drawing.Size(396, 62);
            this.runMiningButton.TabIndex = 8;
            this.runMiningButton.Text = "Run PreEI";
            this.runMiningButton.UseVisualStyleBackColor = true;
            this.runMiningButton.Click += new System.EventHandler(this.OnRunMiningClick);
            // 
            // 
            // 
            // closeButton
            // 
            this.closeButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.closeButton.ForeColor = System.Drawing.SystemColors.Highlight;
            this.closeButton.Location = new System.Drawing.Point(1029, 28);
            this.closeButton.Margin = new System.Windows.Forms.Padding(6);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(139, 62);
            this.closeButton.TabIndex = 9;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.OnCloseClick);
            // 
            // saveResultsButton
            // 
            this.saveResultsButton.Location = new System.Drawing.Point(1018, 258);
            this.saveResultsButton.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.saveResultsButton.Name = "saveResultsButton";
            this.saveResultsButton.Size = new System.Drawing.Size(150, 62);
            this.saveResultsButton.TabIndex = 10;
            this.saveResultsButton.Text = "Save results";
            this.saveResultsButton.UseVisualStyleBackColor = true;
            this.saveResultsButton.Click += new System.EventHandler(this.OnSaveResultsClick);
            // 
            // upperThresholdLabel
            // 
            this.upperThresholdLabel.AutoSize = true;
            this.upperThresholdLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.upperThresholdLabel.ForeColor = System.Drawing.SystemColors.Highlight;
            this.upperThresholdLabel.Location = new System.Drawing.Point(545, 45);
            this.upperThresholdLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.upperThresholdLabel.Name = "upperThresholdLabel";
            this.upperThresholdLabel.Size = new System.Drawing.Size(63, 45);
            this.upperThresholdLabel.TabIndex = 12;
            this.upperThresholdLabel.Text = "Upper (%)";
            // 
            // upperThresholdTextBox
            // 
            this.upperThresholdTextBox.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.upperThresholdTextBox.ForeColor = System.Drawing.SystemColors.Highlight;
            this.upperThresholdTextBox.Location = new System.Drawing.Point(709, 41);
            this.upperThresholdTextBox.Margin = new System.Windows.Forms.Padding(6);
            this.upperThresholdTextBox.Name = "upperThresholdTextBox";
            this.upperThresholdTextBox.Size = new System.Drawing.Size(239, 50);
            this.upperThresholdTextBox.TabIndex = 11;
            this.upperThresholdTextBox.Text = "1.2";
            // 
            // browseDatasetButton
            // 
            this.browseDatasetButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.browseDatasetButton.ForeColor = System.Drawing.SystemColors.Highlight;
            this.browseDatasetButton.Location = new System.Drawing.Point(50, 102);
            this.browseDatasetButton.Margin = new System.Windows.Forms.Padding(6);
            this.browseDatasetButton.Name = "browseDatasetButton";
            this.browseDatasetButton.Size = new System.Drawing.Size(139, 62);
            this.browseDatasetButton.TabIndex = 6;
            this.browseDatasetButton.Text = "Load data";
            this.browseDatasetButton.UseVisualStyleBackColor = true;
            this.browseDatasetButton.Click += new System.EventHandler(this.OnBrowseDatasetClick);
            // 
            // datasetPartsListBox
            // 
            this.datasetPartsListBox.FormattingEnabled = true;
            this.datasetPartsListBox.ItemHeight = 32;
            this.datasetPartsListBox.Location = new System.Drawing.Point(214, 111);
            this.datasetPartsListBox.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.datasetPartsListBox.Name = "datasetPartsListBox";
            this.datasetPartsListBox.Size = new System.Drawing.Size(955, 132);
            this.datasetPartsListBox.TabIndex = 20;
            // 
            // splitConfigurationLabel
            //
            this.splitConfigurationLabel.AutoSize = false;
            this.splitConfigurationLabel.Location = new System.Drawing.Point(49, 184);
            this.splitConfigurationLabel.Name = "splitConfigurationLabel";
            this.splitConfigurationLabel.Size = new System.Drawing.Size(153, 62);
            this.splitConfigurationLabel.TabIndex = 21;
            this.splitConfigurationLabel.Text = "80% initial\n10 batches x 2%";
            //
            // PreEIForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1208, 1449);
            this.Controls.Add(this.splitConfigurationLabel);
            this.Controls.Add(this.datasetPartsListBox);
            this.Controls.Add(this.upperThresholdLabel);
            this.Controls.Add(this.upperThresholdTextBox);
            this.Controls.Add(this.saveResultsButton);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.runMiningButton);
            this.Controls.Add(this.browseDatasetButton);
            this.Controls.Add(this.lowerThresholdLabel);
            this.Controls.Add(this.lowerThresholdTextBox);
            this.Controls.Add(this.resultsTreeView);
            this.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.Name = "PreEIForm";
            this.Text = "PreEI - Incremental Erasable Itemset Mining";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TreeView resultsTreeView;
        private TextBox lowerThresholdTextBox;
        private Label lowerThresholdLabel;
        private Button runMiningButton;
        private Button closeButton;
        private Button saveResultsButton;
        private Label upperThresholdLabel;
        private TextBox upperThresholdTextBox;
        private Button browseDatasetButton;
        private ListBox datasetPartsListBox;
        private Label splitConfigurationLabel;
    }
}