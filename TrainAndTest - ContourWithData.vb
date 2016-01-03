'TrainAndTest
'ContourWithData.vb

Option Explicit On      'require explicit declaration of variables, this is NOT Python !!
Option Strict On        'restrict implicit data type conversions to only widening conversions

Imports Emgu.CV.Util

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Public Class ContourWithData
    
    ' member variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Const MIN_CONTOUR_AREA As Integer = 100

    Public contour As VectorOfPoint             'contour
    Public boundingRect As Rectangle            'bounding rect for contour
    Public dblArea As Double                    'area of contour

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Public Function checkIfContourIsValid() As Boolean      'this is oversimplified, for a production grade program better validity checking would be necessary
        If (dblArea < MIN_CONTOUR_AREA) Then
            Return False
        Else
            Return True
        End If
    End Function

End Class

