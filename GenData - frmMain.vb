'frmMain.vb

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

        Dim imgGrayscale As New Mat()
        Dim imgBlurred As New Mat()
        Dim imgThresh As New Mat()
        Dim imgThreshCopy As New Mat()
        
        Dim contours As New VectorOfVectorOfPoint()
        
        Dim mtxClassifications As Matrix(Of Single)
        Dim mtxTrainingImages As Matrix(Of Single)

        Dim matTrainingImagesAsFlattenedFloats As New Mat()

        Dim intValidChars As New List(Of Integer)(New Integer() { Asc("0"), Asc("1"), Asc("2"), Asc("3"), Asc("4"), Asc("5"), Asc("6"), Asc("7"), Asc("8"), Asc("9"), _
                                                                  Asc("A"), Asc("B"), Asc("C"), Asc("D"), Asc("E"), Asc("F"), Asc("G"), Asc("H"), Asc("I"), Asc("J"), _
                                                                  Asc("K"), Asc("L"), Asc("M"), Asc("N"), Asc("O"), Asc("P"), Asc("Q"), Asc("R"), Asc("S"), Asc("T"), _
                                                                  Asc("U"), Asc("V"), Asc("W"), Asc("X"), Asc("Y"), Asc("Z") } )

        CvInvoke.CvtColor(imgTrainingNumbers, imgGrayscale, ColorConversion.Bgr2Gray)

        CvInvoke.GaussianBlur(imgGrayscale, imgBlurred, New Size(5, 5), 0)

        CvInvoke.AdaptiveThreshold(imgBlurred, imgThresh, 255.0, AdaptiveThresholdType.GaussianC, ThresholdType.BinaryInv, 11, 2)

        CvInvoke.Imshow("imgThresh", imgThresh)

        imgThreshCopy = imgThresh.Clone()

        CvInvoke.FindContours(imgThreshCopy, contours, Nothing, RetrType.External, ChainApproxMethod.ChainApproxSimple)

        Dim intNumberOfTrainingSamples As Integer = contours.Size

        mtxClassifications = New Matrix(Of Single)(intNumberOfTrainingSamples, 1)

        mtxTrainingImages = New Matrix(Of Single)(intNumberOfTrainingSamples, RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT)
        
        Dim intTrainingDataRowToAdd As Integer = 0

        For i As Integer = 0 To contours.Size - 1
            If (CvInvoke.ContourArea(contours(i)) > MIN_CONTOUR_AREA) Then
                Dim boundingRect As Rectangle = CvInvoke.BoundingRectangle(contours(i))

                CvInvoke.Rectangle(imgTrainingNumbers, boundingRect, New MCvScalar(0.0, 0.0, 255.0), 2)

                Dim imgROItoBeCloned As New Mat(imgThresh, boundingRect)

                Dim imgROI As Mat = imgROItoBeCloned.Clone()

                Dim imgROIResized As New Mat()
                CvInvoke.Resize(imgROI, imgROIResized, New Size(RESIZED_IMAGE_WIDTH, RESIZED_IMAGE_HEIGHT))

                CvInvoke.Imshow("imgROI", imgROI)
                CvInvoke.Imshow("imgROIResized", imgROIResized)
                CvInvoke.Imshow("imgTrainingNumbers", imgTrainingNumbers)

                Dim intChar As Integer = CvInvoke.WaitKey(0)

                If (intChar = 27) Then
                    Return
                ElseIf (intValidChars.Contains(intChar)) Then
                    mtxClassifications(intTrainingDataRowToAdd, 0) = Convert.ToSingle(intChar)

                    Dim mtxTemp As Matrix(Of Single) = New Matrix(Of Single)(imgROIResized.Size())
                    Dim mtxTempReshaped As Matrix(Of Single) = New Matrix(Of Single)(1, RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT)
                    
                    imgROIResized.ConvertTo(mtxTemp, DepthType.Cv32F)
                    
                    For intRow As Integer = 0 To RESIZED_IMAGE_HEIGHT - 1
                        For intCol As Integer = 0 To RESIZED_IMAGE_WIDTH - 1
                            mtxTempReshaped(0, (intRow * RESIZED_IMAGE_WIDTH) + intCol) = mtxTemp(intRow, intCol)
                        Next
                    Next

                    For intCol As Integer = 0 To (RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT) - 1
                        mtxTrainingImages(intTrainingDataRowToAdd, intCol) = mtxTempReshaped(0, intCol)
                    Next
                    
                    intTrainingDataRowToAdd = intTrainingDataRowToAdd + 1
                End If
            End If
        Next

        txtInfo.Text = txtInfo.Text + "training complete !!" + vbCrLf + vbCrLf
        
        '            'save classifications to file '''''''''''''''''''''''''''''''''''''''''''''''''''''

        'Dim fsClassifications As New FileStorage("classifications.xml", FileStorage.Mode.Write)

        'If (fsClassifications.IsOpened = False) Then
        '    txtInfo.AppendText("error, unable to open training classifications file, exiting program" + vbCrLf + vbCrLf)
        '    Return
        'End If

        'fsClassifications.Write(mtxClassifications, "classifications",


                    'save classifications to file '''''''''''''''''''''''''''''''''''''''''''''''''''''
        Dim xmlSerializer As XmlSerializer = New XmlSerializer(mtxClassifications.GetType)
        Dim streamWriter As StreamWriter

        Try
            streamWriter = new StreamWriter("classifications.xml")
        Catch ex As Exception
            txtInfo.Text = vbCrLf + txtInfo.Text + "unable to open 'classifications.xml', error:" + vbCrLf
            txtInfo.Text = txtInfo.Text + ex.Message + vbCrLf + vbCrLf
            Return
        End Try

        xmlSerializer.Serialize(streamWriter, mtxClassifications)
        streamWriter.Close()

        'save training images to file '''''''''''''''''''''''''''''''''''''''''''''''''''''

        xmlSerializer = New XmlSerializer(mtxTrainingImages.GetType)

        Try
            streamWriter = new StreamWriter("images.xml")                   'attempt to open images file
        Catch ex As Exception
            txtInfo.Text = vbCrLf + txtInfo.Text + "unable to open 'images.xml', error:" + vbCrLf
            txtInfo.Text = txtInfo.Text + ex.Message + vbCrLf + vbCrLf
            Return
        End Try

        xmlSerializer.Serialize(streamWriter, mtxTrainingImages)
        streamWriter.Close()

        txtInfo.Text = vbCrLf + txtInfo.Text + "file writing done" + vbCrLf

    End Sub

End Class






