'TrainAndTest
'frmMain.vb

'form components:
'frmMain
'tableLayoutPanel
'btnOpenTestImage
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
Imports Emgu.CV.ML              '

Imports System.Xml                  '
Imports System.Xml.Serialization    'these imports are for writing Matrix objects to file, see end of program
Imports System.IO

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Public Class frmMain

    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Const RESIZED_IMAGE_WIDTH As Integer = 20
    Const RESIZED_IMAGE_HEIGHT As Integer = 30

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub btnOpenTestImage_Click(sender As Object, e As EventArgs) Handles btnOpenTestImage.Click
                'note: we effectively have to read the first XML file twice
                'first, we read the file to get the number of rows (which is the same as the number of samples)
                'the first time reading the file we can't get the data yet, since we don't know how many rows of data there are
                'next, reinstantiate our classifications Matrix and training images Matrix with the correct number of rows
                'then, read the file again and this time read the data into our resized classifications Matrix and training images Matrix

        Dim mtxClassifications As Matrix(Of Single) = New Matrix(Of Single)(1, 1)       'for the first time through, declare these to be 1 row by 1 column
        Dim mtxTrainingImages As Matrix(Of Single) = New Matrix(Of Single)(1, 1)        'we will resize these when we know the number of rows (i.e. number of training samples)
        
        Dim intValidChars As New List(Of Integer)(New Integer() { Asc("0"), Asc("1"), Asc("2"), Asc("3"), Asc("4"), Asc("5"), Asc("6"), Asc("7"), Asc("8"), Asc("9"), _
                                                                  Asc("A"), Asc("B"), Asc("C"), Asc("D"), Asc("E"), Asc("F"), Asc("G"), Asc("H"), Asc("I"), Asc("J"), _
                                                                  Asc("K"), Asc("L"), Asc("M"), Asc("N"), Asc("O"), Asc("P"), Asc("Q"), Asc("R"), Asc("S"), Asc("T"), _
                                                                  Asc("U"), Asc("V"), Asc("W"), Asc("X"), Asc("Y"), Asc("Z") } )

        Dim xmlSerializer As XmlSerializer = New XmlSerializer(mtxClassifications.GetType)          'these variables are for
        Dim streamReader As StreamReader                                                            'reading from the XML files

        Try
            streamReader = new StreamReader("classifications.xml")                          'attempt to open classifications file
        Catch ex As Exception                                                               'if error is encountered, show error and return
            txtInfo.AppendText(vbCrLf + "unable to open 'classifications.xml', error: ")
            txtInfo.AppendText(ex.Message + vbCrLf)
            Return
        End Try
                    'read from the classifications file the 1st time, this is only to get the number of rows, not the actual data
        mtxClassifications = CType(xmlSerializer.Deserialize(streamReader), Matrix(Of Single))

        streamReader.Close()            'close the classifications XML file

        Dim intNumberOfTrainingSamples As Integer = mtxClassifications.Rows         'get the number of rows, i.e. the number of training samples

                'now that we know the number of rows, reinstantiate classifications Matrix and training images Matrix with the actual number of rows
        mtxClassifications = New Matrix(Of Single)(intNumberOfTrainingSamples, 1)
        mtxTrainingImages = New Matrix(Of Single)(intNumberOfTrainingSamples, RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT)

        Try
            streamReader = new StreamReader("classifications.xml")                      'reinitialize the stream reader, attempt to open classifications file again
        Catch ex As Exception                                                           'if error is encountered, show error and return
            txtInfo.AppendText(vbCrLf + "unable to open 'classifications.xml', error:" + vbCrLf)
            txtInfo.AppendText(ex.Message + vbCrLf + vbCrLf)
            Return
        End Try
                        'read from the classifications file again, this time we can get the actual data
        mtxClassifications = CType(xmlSerializer.Deserialize(streamReader), Matrix(Of Single))

        streamReader.Close()                'close the classifications XML file

        xmlSerializer = New XmlSerializer(mtxTrainingImages.GetType)                'reinstantiate file reading variable

        Try
            streamReader = New StreamReader("images.xml")                               'attempt to open classifications file
        Catch ex As Exception                                                           'if error is encountered, show error and return
            txtInfo.AppendText("unable to open 'images.xml', error:" + vbCrLf)
            txtInfo.AppendText(ex.Message + vbCrLf + vbCrLf)
        End Try

        mtxTrainingImages = CType(xmlSerializer.Deserialize(streamReader), Matrix(Of Single))       'read from training images file
        streamReader.Close()                                                                        'close the training images XML file

                    ' train '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim kNearest As New KNearest()

        kNearest.DefaultK = 1

        kNearest.Train(mtxTrainingImages, MlEnum.DataLayoutType.RowSample, mtxClassifications)

                    ' test '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim drChosenFile As DialogResult

        drChosenFile = ofdOpenFile.ShowDialog()                 'open file dialog

        If (drChosenFile <> DialogResult.OK Or ofdOpenFile.FileName = "") Then
            lblChosenFile.Text = "file not chosen"              'show error message on label
            Return
        End If

        Dim imgTestingNumbers As Mat            'declare the input image

        Try
            imgTestingNumbers = CvInvoke.Imread(ofdOpenFile.FileName, LoadImageType.Color)      'open image
        Catch ex As Exception                                                                   'if error occurred
            lblChosenFile.Text = "unable to open image, error: " + ex.Message                   'show error message on label
            Return                                                                              'and exit function
        End Try

        If (imgTestingNumbers Is Nothing) Then                          'if image could not be opened
            lblChosenFile.Text = "unable to open image"                 'show error message on label
            Return                                                      'and exit function
        End If

        If (imgTestingNumbers.IsEmpty()) Then
            lblChosenFile.Text = "unable to open image"
            Return
        End If

        lblChosenFile.Text = ofdOpenFile.FileName               'update label with file name

        Dim imgGrayscale As New Mat()                   '
        Dim imgBlurred As New Mat()                     'declare various images
        Dim imgThresh As New Mat()                      '
        Dim imgThreshCopy As New Mat()                  '

        CvInvoke.CvtColor(imgTestingNumbers, imgGrayscale, ColorConversion.Bgr2Gray)        'convert to grayscale

        CvInvoke.GaussianBlur(imgGrayscale, imgBlurred, New Size(5, 5), 0)                  'blur

                            'threshold image from grayscale to black and white
        CvInvoke.AdaptiveThreshold(imgBlurred, imgThresh, 255.0, AdaptiveThresholdType.GaussianC, ThresholdType.BinaryInv, 11, 2.0)

        imgThreshCopy = imgThresh.Clone()           'make a copy of the thresh image, this in necessary b/c findContours modifies the image

        Dim contours As New VectorOfVectorOfPoint()

                        'get external countours only
        CvInvoke.FindContours(imgThreshCopy, contours, Nothing, RetrType.External, ChainApproxMethod.ChainApproxSimple)

        Dim listOfContoursWithData As New List(Of ContourWithData)          'declare a list of contours with data

                                                                    'populate list of contours with data
        For i As Integer = 0 To contours.Size - 1                   'for each contour
            Dim contourWithData As New ContourWithData                              'declare new contour with data
            contourWithData.contour = contours(i)                                                   'populate contour member variable
            contourWithData.boundingRect = CvInvoke.BoundingRectangle(contourWithData.contour)      'calculate bounding rectangle
            contourWithData.dblArea = CvInvoke.ContourArea(contourWithData.contour)                 'calculate area
            If (contourWithData.checkIfContourIsValid()) Then                                       'if contour with data is valis
                listOfContoursWithData.Add(contourWithData)                                         'add to list of contours with data
            End If
        Next
                        'sort contours with data from left to right
        listOfContoursWithData.Sort(Function(oneContourWithData, otherContourWithData) oneContourWithData.boundingRect.X.CompareTo(otherContourWithData.boundingRect.X))

        Dim strFinalString As String = ""           'declare final string, this will have the final number sequence by the end of the program

        For Each contourWithData As ContourWithData In listOfContoursWithData               'for each contour in list of valid contours

            CvInvoke.Rectangle(imgTestingNumbers, contourWithData.boundingRect, New MCvScalar(0.0, 255.0, 0.0), 2)      'draw green rect around the current char

            Dim imgROItoBeCloned As New Mat(imgThresh, contourWithData.boundingRect)        'get ROI image of bounding rect

            Dim imgROI As Mat = imgROItoBeCloned.Clone()                'clone ROI image so we don't change original when we resize

            Dim imgROIResized As New Mat()

                    'resize image, this is necessary for char recognition
            CvInvoke.Resize(imgROI, imgROIResized, New Size(RESIZED_IMAGE_WIDTH, RESIZED_IMAGE_HEIGHT))

                    'declare a Matrix of the same dimensions as the Image we are adding to the data structure of training images
            Dim mtxTemp As Matrix(Of Single) = New Matrix(Of Single)(imgROIResized.Size())

                    'declare a flattened (only 1 row) matrix of the same total size
            Dim mtxTempReshaped As Matrix(Of Single) = New Matrix(Of Single)(1, RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT)

            imgROIResized.ConvertTo(mtxTemp, DepthType.Cv32F)           'convert Image to a Matrix of Singles with the same dimensions

            For intRow As Integer = 0 To RESIZED_IMAGE_HEIGHT - 1       'flatten Matrix into one row by RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT number of columns
                For intCol As Integer = 0 To RESIZED_IMAGE_WIDTH - 1
                    mtxTempReshaped(0, (intRow * RESIZED_IMAGE_WIDTH) + intCol) = mtxTemp(intRow, intCol)
                Next
            Next
            
            Dim sngCurrentChar As Single

            sngCurrentChar = kNearest.Predict(mtxTempReshaped)              'finally we can call Predict !!!

            strFinalString = strFinalString + Chr(Convert.ToInt32(sngCurrentChar))          'append current char to full string of chars
            
        Next

        txtInfo.AppendText(vbCrLf + vbCrLf + "characters read from image = " + strFinalString + vbCrLf)

        CvInvoke.Imshow("imgTestingNumbers", imgTestingNumbers)

    End Sub
    
End Class
