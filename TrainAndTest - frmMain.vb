'frmMain.vb

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
Public Class ContourWithData

    ' member variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Const MIN_CONTOUR_AREA As Integer = 100

    Public contour As VectorOfPoint
    Public boundingRect As Rectangle
    Public dblArea As Double

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Public Function checkIfContourIsValid() As Boolean
        If (dblArea < MIN_CONTOUR_AREA) Then
            Return False
        Else
            Return True
        End If
    End Function
    
End Class

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Public Class frmMain

    ' module level variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Const RESIZED_IMAGE_WIDTH As Integer = 20
    Const RESIZED_IMAGE_HEIGHT As Integer = 30

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub btnOpenTestImage_Click(sender As Object, e As EventArgs) Handles btnOpenTestImage.Click

        Dim mtxClassifications As Matrix(Of Single) = New Matrix(Of Single)(1, 1)
        Dim mtxTrainingImages As Matrix(Of Single) = New Matrix(Of Single)(1, 1)

        Dim intValidChars As New List(Of Integer)(New Integer() { Asc("0"), Asc("1"), Asc("2"), Asc("3"), Asc("4"), Asc("5"), Asc("6"), Asc("7"), Asc("8"), Asc("9"), _
                                                                  Asc("A"), Asc("B"), Asc("C"), Asc("D"), Asc("E"), Asc("F"), Asc("G"), Asc("H"), Asc("I"), Asc("J"), _
                                                                  Asc("K"), Asc("L"), Asc("M"), Asc("N"), Asc("O"), Asc("P"), Asc("Q"), Asc("R"), Asc("S"), Asc("T"), _
                                                                  Asc("U"), Asc("V"), Asc("W"), Asc("X"), Asc("Y"), Asc("Z") } )

        Dim xmlSerializer As XmlSerializer = New XmlSerializer(mtxClassifications.GetType)
        Dim streamReader As StreamReader

        Try
            streamReader = new StreamReader("classifications.xml")
        Catch ex As Exception
            txtInfo.AppendText(vbCrLf + "unable to open 'classifications.xml', error: ")
            txtInfo.AppendText(ex.Message + vbCrLf)
            Return
        End Try

        mtxClassifications = CType(xmlSerializer.Deserialize(streamReader), Matrix(Of Single))

        streamReader.Close()

        Dim intNumberOfTrainingSamples As Integer = mtxClassifications.Rows

        mtxClassifications = New Matrix(Of Single)(intNumberOfTrainingSamples, 1)
        mtxTrainingImages = New Matrix(Of Single)(intNumberOfTrainingSamples, RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT)

        Try
            streamReader = new StreamReader("classifications.xml")
        Catch ex As Exception
            txtInfo.AppendText(vbCrLf + "unable to open 'classifications.xml', error:" + vbCrLf)
            txtInfo.AppendText(ex.Message + vbCrLf + vbCrLf)
            Return
        End Try

        mtxClassifications = CType(xmlSerializer.Deserialize(streamReader), Matrix(Of Single))

        streamReader.Close()

        xmlSerializer = New XmlSerializer(mtxTrainingImages.GetType)

        Try
            streamReader = New StreamReader("images.xml")
        Catch ex As Exception
            txtInfo.AppendText("unable to open 'images.xml', error:" + vbCrLf)
            txtInfo.AppendText(ex.Message + vbCrLf + vbCrLf)
        End Try

        mtxTrainingImages = CType(xmlSerializer.Deserialize(streamReader), Matrix(Of Single))
        streamReader.Close()

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

        Dim imgTestingNumbers As Mat

        Try
            imgTestingNumbers = CvInvoke.Imread(ofdOpenFile.FileName, LoadImageType.Color)
        Catch ex As Exception
            lblChosenFile.Text = "unable to open image, error: " + ex.Message       'show error message on label
            Return
        End Try

        If (imgTestingNumbers Is Nothing) Then
            lblChosenFile.Text = "unable to open image"
            Return
        End If

        If (imgTestingNumbers.IsEmpty()) Then
            lblChosenFile.Text = "unable to open image"
            Return
        End If

        lblChosenFile.Text = ofdOpenFile.FileName

        Dim imgGrayscale As New Mat()
        Dim imgBlurred As New Mat()
        Dim imgThresh As New Mat()
        Dim imgThreshCopy As New Mat()

        CvInvoke.CvtColor(imgTestingNumbers, imgGrayscale, ColorConversion.Bgr2Gray)

        CvInvoke.GaussianBlur(imgGrayscale, imgBlurred, New Size(5, 5), 0)

        CvInvoke.AdaptiveThreshold(imgBlurred, imgThresh, 255.0, AdaptiveThresholdType.GaussianC, ThresholdType.BinaryInv, 11, 2.0)

        imgThreshCopy = imgThresh.Clone()

        Dim contours As New VectorOfVectorOfPoint()

        CvInvoke.FindContours(imgThreshCopy, contours, Nothing, RetrType.External, ChainApproxMethod.ChainApproxSimple)

        Dim listOfContoursWithData As New List(Of ContourWithData)

        For i As Integer = 0 To contours.Size - 1
            Dim contourWithData As New ContourWithData
            contourWithData.contour = contours(i)
            contourWithData.boundingRect = CvInvoke.BoundingRectangle(contourWithData.contour)
            contourWithData.dblArea = CvInvoke.ContourArea(contourWithData.contour)
            If (contourWithData.checkIfContourIsValid()) Then
                listOfContoursWithData.Add(contourWithData)
            End If
        Next

        listOfContoursWithData.Sort(Function(oneContourWithData, otherContourWithData) oneContourWithData.boundingRect.X.CompareTo(otherContourWithData.boundingRect.X))

        Dim strFinalString As String = ""

        For Each contourWithData As ContourWithData In listOfContoursWithData

            CvInvoke.Rectangle(imgTestingNumbers, contourWithData.boundingRect, New MCvScalar(0.0, 255.0, 0.0), 2)

            Dim imgROItoBeCloned As New Mat(imgThresh, contourWithData.boundingRect)

            Dim imgROI As Mat = imgROItoBeCloned.Clone()

            Dim imgROIResized As New Mat()

            CvInvoke.Resize(imgROI, imgROIResized, New Size(RESIZED_IMAGE_WIDTH, RESIZED_IMAGE_HEIGHT))

            Dim mtxTemp As Matrix(Of Single) = New Matrix(Of Single)(imgROIResized.Size())
            Dim mtxTempReshaped As Matrix(Of Single) = New Matrix(Of Single)(1, RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT)

            imgROIResized.ConvertTo(mtxTemp, DepthType.Cv32F)

            For intRow As Integer = 0 To RESIZED_IMAGE_HEIGHT - 1
                For intCol As Integer = 0 To RESIZED_IMAGE_WIDTH - 1
                    mtxTempReshaped(0, (intRow * RESIZED_IMAGE_WIDTH) + intCol) = mtxTemp(intRow, intCol)
                Next
            Next

            Dim sngCurrentChar As Single

            sngCurrentChar = kNearest.Predict(mtxTempReshaped)

            strFinalString = strFinalString + Chr(Convert.ToInt32(sngCurrentChar))
            
        Next

        txtInfo.AppendText(vbCrLf + vbCrLf + "characters read from image = " + strFinalString + vbCrLf)

        CvInvoke.Imshow("imgTestingNumbers", imgTestingNumbers)
        
    End Sub
    
End Class




