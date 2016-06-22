// GenerateData.cs

// using Emgu CV 2.4.10

// add the following components to your form:
// btnOpenTrainingImage (Button)
// lblChosenFile (Label)
// txtInfo (TextBox)
// ofdOpenFile (OpenFileDialog)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Emgu.CV;                      //
using Emgu.CV.CvEnum;               // Emgu Cv imports
using Emgu.CV.Structure;            //
using Emgu.CV.UI;                   //
using Emgu.CV.ML;                   //
using Emgu.CV.Util;

using System.Xml;                   //
using System.Xml.Serialization;     // these imports areE for writing Matrix objects to file, see end of program
using System.IO;                    //

///////////////////////////////////////////////////////////////////////////////////////////////////
namespace GenerateData4 {

    ///////////////////////////////////////////////////////////////////////////////////////////////
    public partial class frmMain : Form {

        // module level variables /////////////////////////////////////////////////////////////////
        const int MIN_CONTOUR_AREA = 100;

        const int RESIZED_IMAGE_WIDTH = 20;
        const int RESIZED_IMAGE_HEIGHT = 30;
        private Button btnOpenTrainingImage;
        private Label lblChosenFile;
        private TextBox txtInfo;
        private OpenFileDialog ofdOpenFile;
        private TableLayoutPanel tableLayoutPanel1;

        // constructor ////////////////////////////////////////////////////////////////////////////
        public frmMain() {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.btnOpenTrainingImage = new System.Windows.Forms.Button();
            this.lblChosenFile = new System.Windows.Forms.Label();
            this.txtInfo = new System.Windows.Forms.TextBox();
            this.ofdOpenFile = new System.Windows.Forms.OpenFileDialog();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOpenTrainingImage
            // 
            this.btnOpenTrainingImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenTrainingImage.AutoSize = true;
            this.btnOpenTrainingImage.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnOpenTrainingImage.Location = new System.Drawing.Point(3, 3);
            this.btnOpenTrainingImage.Name = "btnOpenTrainingImage";
            this.btnOpenTrainingImage.Size = new System.Drawing.Size(116, 23);
            this.btnOpenTrainingImage.TabIndex = 0;
            this.btnOpenTrainingImage.Text = "Open Training Image";
            this.btnOpenTrainingImage.UseVisualStyleBackColor = true;
            this.btnOpenTrainingImage.Click += new System.EventHandler(this.btnOpenTrainingImage_Click);
            // 
            // lblChosenFile
            // 
            this.lblChosenFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblChosenFile.AutoSize = true;
            this.lblChosenFile.Location = new System.Drawing.Point(125, 8);
            this.lblChosenFile.Name = "lblChosenFile";
            this.lblChosenFile.Size = new System.Drawing.Size(373, 13);
            this.lblChosenFile.TabIndex = 1;
            this.lblChosenFile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtInfo
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.txtInfo, 2);
            this.txtInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtInfo.Location = new System.Drawing.Point(3, 32);
            this.txtInfo.Multiline = true;
            this.txtInfo.Name = "txtInfo";
            this.txtInfo.Size = new System.Drawing.Size(495, 220);
            this.txtInfo.TabIndex = 2;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.btnOpenTrainingImage, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.txtInfo, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.lblChosenFile, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(501, 255);
            this.tableLayoutPanel1.TabIndex = 3;
            // 
            // frmMain
            // 
            this.ClientSize = new System.Drawing.Size(501, 255);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "frmMain";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }


        static class Program
        {
            /// <summary>
            /// The main entry point for the application.
            /// </summary>
            [STAThread]
            static void Main()
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new frmMain());
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        private void btnOpenTrainingImage_Click(object sender, EventArgs e) {

            DialogResult drChosenFile;
            drChosenFile = ofdOpenFile.ShowDialog();
            // open file dialog
            if (((drChosenFile != DialogResult.OK)
                        || (ofdOpenFile.FileName == "")))
            {
                // if user chose Cancel or filename is blank . . .
                lblChosenFile.Text = "file not chosen";
                return;
            }

            Mat imgTrainingNumbers;
            try
            {
                imgTrainingNumbers = CvInvoke.Imread(ofdOpenFile.FileName, LoadImageType.Color);
            }
            catch (Exception ex)
            {
                // if error occurred
                lblChosenFile.Text = ("unable to open image, error: " + ex.Message);
                // show error message on label
                return;
            }

            if ((imgTrainingNumbers == null))
            {
                // if image could not be opened
                lblChosenFile.Text = "unable to open image";
                return;
            }

            lblChosenFile.Text = ofdOpenFile.FileName;
            // update label with file name
            Mat imgGrayscale = new Mat();
            // 
            Mat imgBlurred = new Mat();
            // declare various images
            Mat imgThresh = new Mat();
            // 
            Mat imgThreshCopy = new Mat();
            // 
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();

            List<int> intValidChars = new List<int> { (int)'0', (int)'1', (int)'2', (int)'3', (int)'4', (int)'5', (int)'6', (int)'7', (int)'8', (int)'9' ,
                                                      (int)'A', (int)'B', (int)'C', (int)'D', (int)'E', (int)'F', (int)'G', (int)'H', (int)'I', (int)'J' ,
            (int)'K', (int)'L', (int)'M', (int)'N', (int)'O', (int)'P', (int)'Q', (int)'R', (int)'S', (int)'T',(int)'U', (int)'V', (int)'W', (int)'X', (int)'Y', (int)'Z'};
            Matrix<Single> mtxClassifications;
            Matrix<Single> mtxTrainingImages;
            Mat matTrainingImagesAsFlattenedFloats = new Mat();
            // possible chars we are interested in are digits 0 through 9 and capital letters A through Z, put these in list intValidChars

            CvInvoke.CvtColor(imgTrainingNumbers, imgGrayscale, ColorConversion.Bgr2Gray);
            // convert to grayscale
            CvInvoke.GaussianBlur(imgGrayscale, imgBlurred, new Size(5, 5), 0);
            // blur
            // threshold image from grayscale to black and white
            CvInvoke.AdaptiveThreshold(imgBlurred, imgThresh, 255, AdaptiveThresholdType.GaussianC, ThresholdType.BinaryInv, 11, 2);

            CvInvoke.Imshow("imgThresh", imgThresh);
            // show threshold image for reference
            imgThreshCopy = imgThresh.Clone();
            // make a copy of the thresh image, this in necessary b/c findContours modifies the image
            // get external countours only
            CvInvoke.FindContours(imgThreshCopy, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            int intNumberOfTrainingSamples = contours.Size;
            mtxClassifications = new Matrix<float>(intNumberOfTrainingSamples, 1);
            // this is our classifications data structure
            // this is our training images data structure, note we will have to perform some conversions to write to this later
            mtxTrainingImages = new Matrix<float>(intNumberOfTrainingSamples, (RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT));
            // this keeps track of which row we are on in both classifications and training images,
            int intTrainingDataRowToAdd = 0;
            // note that each sample will correspond to one row in
            // both the classifications XML file and the training images XML file
            for (int i = 0; (i
                        <= (contours.Size - 1)); i++)
            {
                // for each contour
                if ((CvInvoke.ContourArea(contours[i]) > MIN_CONTOUR_AREA))
                {
                    // if contour is big enough to consider
                    Rectangle boundingRect = CvInvoke.BoundingRectangle(contours[i]);
                    // get the bounding rect
                    CvInvoke.Rectangle(imgTrainingNumbers, boundingRect, new MCvScalar(0, 0, 255), 2);
                    // draw red rectangle around each contour as we ask user for input
                    Mat imgROItoBeCloned = new Mat(imgThresh, boundingRect);
                    // get ROI image of current char
                    Mat imgROI = imgROItoBeCloned.Clone();
                    // make a copy so we do not change the ROI area of the original image
                    Mat imgROIResized = new Mat();
                    // resize image, this is necessary for recognition and storage
                    CvInvoke.Resize(imgROI, imgROIResized, new Size(RESIZED_IMAGE_WIDTH, RESIZED_IMAGE_HEIGHT));
                    CvInvoke.Imshow("imgROI", imgROI);
                    // show ROI image for reference
                    CvInvoke.Imshow("imgROIResized", imgROIResized);
                    // show resized ROI image for reference
                    CvInvoke.Imshow("imgTrainingNumbers", imgTrainingNumbers);
                    // show training numbers image, this will now have red rectangles drawn on it
                    int intChar = CvInvoke.WaitKey(0);
                    // get key press
                    if ((intChar == 27))
                    {
                        // if esc key was pressed
                        CvInvoke.DestroyAllWindows();
                        return;
                    }
                    else if (intValidChars.Contains(intChar))
                    {
                        // else if the char is in the list of chars we are looking for . . .
                        mtxClassifications[intTrainingDataRowToAdd, 0] = Convert.ToSingle(intChar);
                        // write classification char to classifications Matrix
                        // now add the training image (some conversion is necessary first) . . .
                        // note that we have to covert the images to Matrix(Of Single) type, this is necessary to pass into the KNearest object call to train
                        Matrix<Single> mtxTemp = new Matrix<Single>(imgROIResized.Size);
                        Matrix<Single> mtxTempReshaped = new Matrix<Single>(1, (RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT));
                        imgROIResized.ConvertTo(mtxTemp, DepthType.Cv32F);
                        // convert Image to a Matrix of Singles with the same dimensions
                        for (int intRow = 0; (intRow
                                    <= (RESIZED_IMAGE_HEIGHT - 1)); intRow++)
                        {
                            // flatten Matrix into one row by RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT number of columns
                            for (int intCol = 0; (intCol
                                        <= (RESIZED_IMAGE_WIDTH - 1)); intCol++)
                            {
                                mtxTempReshaped[0, ((intRow * RESIZED_IMAGE_WIDTH)
                                            + intCol)] = mtxTemp[intRow, intCol];
                            }

                        }

                        for (int intCol = 0; (intCol
                                    <= ((RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT)
                                    - 1)); intCol++)
                        {
                            // write flattened Matrix into one row of training images Matrix
                            mtxTrainingImages[intTrainingDataRowToAdd, intCol] = mtxTempReshaped[0, intCol];
                        }

                        intTrainingDataRowToAdd = (intTrainingDataRowToAdd + 1);
                        // increment which row, i.e. sample we are on
                    }

                }

            }

            txtInfo.Text = (txtInfo.Text + ("training complete !!" + ("\r\n" + "\r\n")));
            // save classifications to file '''''''''''''''''''''''''''''''''''''''''''''''''''''
            XmlSerializer xmlSerializer = new XmlSerializer(mtxClassifications.GetType());
            StreamWriter streamWriter;
            try
            {
                streamWriter = new StreamWriter("classifications.xml");
                // attempt to open classifications file
            }
            catch (Exception ex)
            {
                // if error is encountered, show error and return
                txtInfo.Text = ("\r\n"
                            + (txtInfo.Text + ("unable to open \'classifications.xml\', error:" + "\r\n")));
                txtInfo.Text = (txtInfo.Text
                            + (ex.Message + ("\r\n" + "\r\n")));
                return;
            }

            xmlSerializer.Serialize(streamWriter, mtxClassifications);
            streamWriter.Close();
            // save training images to file '''''''''''''''''''''''''''''''''''''''''''''''''''''
            xmlSerializer = new XmlSerializer(mtxTrainingImages.GetType());
            try
            {
                streamWriter = new StreamWriter("images.xml");
                // attempt to open images file
            }
            catch (Exception ex)
            {
                // if error is encountered, show error and return
                txtInfo.Text = ("\r\n"
                            + (txtInfo.Text + ("unable to open \'images.xml\', error:" + "\r\n")));
                txtInfo.Text = (txtInfo.Text
                            + (ex.Message + ("\r\n" + "\r\n")));
                return;
            }
            xmlSerializer.Serialize(streamWriter, mtxTrainingImages);
            streamWriter.Close();

            txtInfo.Text = Environment.NewLine + txtInfo.Text + "file writing done" + Environment.NewLine;
        }

        
        

    }   // end class

}   // end namespace
