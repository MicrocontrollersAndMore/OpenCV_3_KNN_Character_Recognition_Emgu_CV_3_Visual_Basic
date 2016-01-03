'GenData
'frmMain.vb

'form components:
'frmMain
'tableLayoutPanel
'btnOpenTrainingImage
'lblChosenFile
'txtInfo
'ofdOpenFile

Option Explicit On      'require explicit declaration of variables, this is NOT Python !!
Option Strict On        'restrict implicit data type conversions to only widening conversions

Imports Emgu.CV                 'usual Emgu Cv imports
Imports Emgu.CV.CvEnum          '
Imports Emgu.CV.Structure       '
Imports Emgu.CV.UI              '
Imports Emgu.CV.Util            '

Imports System.Xml                  '
Imports System.Xml.Serialization    'these imports are for writing Matrix objects to file, see end of program
Imports System.IO                   '

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Public Class frmMain

    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Const MIN_CONTOUR_AREA As Integer = 100
    
    Const RESIZED_IMAGE_WIDTH As Integer = 20
    Const RESIZED_IMAGE_HEIGHT As Integer = 30

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub btnOpenTrainingImage_Click(sender As Object, e As EventArgs) Handles btnOpenTrainingImage.Click
        Dim drChosenFile As DialogResult

        drChosenFile = ofdOpenFile.ShowDialog()                 'open file dialog

        If (drChosenFile <> DialogResult.OK Or ofdOpenFile.FileName = "") Then    'if user chose Cancel or filename is blank . . .
            lblChosenFile.Text = "file not chosen"              'show error message on label
            Return                                              'and exit function
        End If

        Dim imgTrainingNumbers As Mat

        Try
            imgTrainingNumbers = CvInvoke.Imread(ofdOpenFile.FileName, LoadImageType.Color)
        Catch ex As Exception                                                       'if error occurred
            lblChosenFile.Text = "unable to open image, error: " + ex.Message       'show error message on label
            Return                                                                  'and exit function
        End Try

        If (imgTrainingNumbers Is Nothing) Then                                  'if image could not be opened
            lblChosenFile.Text = "unable to open image"                 'show error message on label
            Return                                                      'and exit function
        End If

        lblChosenFile.Text = ofdOpenFile.FileName           'update label with file name

        Dim imgGrayscale As New Mat()               '
        Dim imgBlurred As New Mat()                 'declare various images
        Dim imgThresh As New Mat()                  '
        Dim imgThreshCopy As New Mat()              '
        
        Dim contours As New VectorOfVectorOfPoint()
        
        Dim mtxClassifications As Matrix(Of Single)
        Dim mtxTrainingImages As Matrix(Of Single)

        Dim matTrainingImagesAsFlattenedFloats As New Mat()

                'possible chars we are interested in are digits 0 through 9 and capital letters A through Z, put these in list intValidChars
        Dim intValidChars As New List(Of Integer)(New Integer() { Asc("0"), Asc("1"), Asc("2"), Asc("3"), Asc("4"), Asc("5"), Asc("6"), Asc("7"), Asc("8"), Asc("9"), _
                                                                  Asc("A"), Asc("B"), Asc("C"), Asc("D"), Asc("E"), Asc("F"), Asc("G"), Asc("H"), Asc("I"), Asc("J"), _
                                                                  Asc("K"), Asc("L"), Asc("M"), Asc("N"), Asc("O"), Asc("P"), Asc("Q"), Asc("R"), Asc("S"), Asc("T"), _
                                                                  Asc("U"), Asc("V"), Asc("W"), Asc("X"), Asc("Y"), Asc("Z") } )

        CvInvoke.CvtColor(imgTrainingNumbers, imgGrayscale, ColorConversion.Bgr2Gray)       'convert to grayscale

        CvInvoke.GaussianBlur(imgGrayscale, imgBlurred, New Size(5, 5), 0)                  'blur

                                                                                            'threshold image from grayscale to black and white
        CvInvoke.AdaptiveThreshold(imgBlurred, imgThresh, 255.0, AdaptiveThresholdType.GaussianC, ThresholdType.BinaryInv, 11, 2)

        CvInvoke.Imshow("imgThresh", imgThresh)                 'show threshold image for reference

        imgThreshCopy = imgThresh.Clone()               'make a copy of the thresh image, this in necessary b/c findContours modifies the image

                    'get external countours only
        CvInvoke.FindContours(imgThreshCopy, contours, Nothing, RetrType.External, ChainApproxMethod.ChainApproxSimple)

        Dim intNumberOfTrainingSamples As Integer = contours.Size

        mtxClassifications = New Matrix(Of Single)(intNumberOfTrainingSamples, 1)       'this is our classifications data structure

                'this is our training images data structure, note we will have to perform some conversions to write to this later
        mtxTrainingImages = New Matrix(Of Single)(intNumberOfTrainingSamples, RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT)
        
                                                            'this keeps track of which row we are on in both classifications and training images,
        Dim intTrainingDataRowToAdd As Integer = 0          'note that each sample will correspond to one row in
                                                            'both the classifications XML file and the training images XML file

        For i As Integer = 0 To contours.Size - 1                               'for each contour
            If (CvInvoke.ContourArea(contours(i)) > MIN_CONTOUR_AREA) Then                      'if contour is big enough to consider
                Dim boundingRect As Rectangle = CvInvoke.BoundingRectangle(contours(i))                 'get the bounding rect

                CvInvoke.Rectangle(imgTrainingNumbers, boundingRect, New MCvScalar(0.0, 0.0, 255.0), 2)     'draw red rectangle around each contour as we ask user for input

                Dim imgROItoBeCloned As New Mat(imgThresh, boundingRect)        'get ROI image of current char

                Dim imgROI As Mat = imgROItoBeCloned.Clone()            'make a copy so we do not change the ROI area of the original image

                Dim imgROIResized As New Mat()
                                                                        'resize image, this is necessary for recognition and storage
                CvInvoke.Resize(imgROI, imgROIResized, New Size(RESIZED_IMAGE_WIDTH, RESIZED_IMAGE_HEIGHT))

                CvInvoke.Imshow("imgROI", imgROI)                                   'show ROI image for reference
                CvInvoke.Imshow("imgROIResized", imgROIResized)                     'show resized ROI image for reference
                CvInvoke.Imshow("imgTrainingNumbers", imgTrainingNumbers)           'show training numbers image, this will now have red rectangles drawn on it

                Dim intChar As Integer = CvInvoke.WaitKey(0)                'get key press

                If (intChar = 27) Then                                      'if esc key was pressed
                    CvInvoke.DestroyAllWindows()
                    Return                                                  'exit the function
                ElseIf (intValidChars.Contains(intChar)) Then               'else if the char is in the list of chars we are looking for . . .

                    mtxClassifications(intTrainingDataRowToAdd, 0) = Convert.ToSingle(intChar)          'write classification char to classifications Matrix

                                'now add the training image (some conversion is necessary first) . . .
                                'note that we have to covert the images to Matrix(Of Single) type, this is necessary to pass into the KNearest object call to train
                    Dim mtxTemp As Matrix(Of Single) = New Matrix(Of Single)(imgROIResized.Size())
                    Dim mtxTempReshaped As Matrix(Of Single) = New Matrix(Of Single)(1, RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT)
                    
                    imgROIResized.ConvertTo(mtxTemp, DepthType.Cv32F)           'convert Image to a Matrix of Singles with the same dimensions
                    
                    For intRow As Integer = 0 To RESIZED_IMAGE_HEIGHT - 1           'flatten Matrix into one row by RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT number of columns
                        For intCol As Integer = 0 To RESIZED_IMAGE_WIDTH - 1
                            mtxTempReshaped(0, (intRow * RESIZED_IMAGE_WIDTH) + intCol) = mtxTemp(intRow, intCol)
                        Next
                    Next

                    For intCol As Integer = 0 To (RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT) - 1           'write flattened Matrix into one row of training images Matrix
                        mtxTrainingImages(intTrainingDataRowToAdd, intCol) = mtxTempReshaped(0, intCol)
                    Next
                    
                    intTrainingDataRowToAdd = intTrainingDataRowToAdd + 1           'increment which row, i.e. sample we are on
                End If
            End If
        Next

        txtInfo.Text = txtInfo.Text + "training complete !!" + vbCrLf + vbCrLf

                'save classifications to file '''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim xmlSerializer As XmlSerializer = New XmlSerializer(mtxClassifications.GetType)
        Dim streamWriter As StreamWriter

        Try
            streamWriter = new StreamWriter("classifications.xml")              'attempt to open classifications file
        Catch ex As Exception                                                   'if error is encountered, show error and return
            txtInfo.Text = vbCrLf + txtInfo.Text + "unable to open 'classifications.xml', error:" + vbCrLf
            txtInfo.Text = txtInfo.Text + ex.Message + vbCrLf + vbCrLf
            Return
        End Try

        xmlSerializer.Serialize(streamWriter, mtxClassifications)
        streamWriter.Close()

                'save training images to file '''''''''''''''''''''''''''''''''''''''''''''''''''''

        xmlSerializer = New XmlSerializer(mtxTrainingImages.GetType)

        Try
            streamWriter = new StreamWriter("images.xml")                       'attempt to open images file
        Catch ex As Exception                                                   'if error is encountered, show error and return
            txtInfo.Text = vbCrLf + txtInfo.Text + "unable to open 'images.xml', error:" + vbCrLf
            txtInfo.Text = txtInfo.Text + ex.Message + vbCrLf + vbCrLf
            Return
        End Try

        xmlSerializer.Serialize(streamWriter, mtxTrainingImages)
        streamWriter.Close()

        txtInfo.Text = vbCrLf + txtInfo.Text + "file writing done" + vbCrLf

        MsgBox("Training complete, file writing done !!")
        
    End Sub
    
End Class
