type
    TPoint = record
        x, y: double;
    end;
    TCurve = array [1..100] of TPoint;
    TDCurve = record
        a, b: TCurve;
    end;
        
var
    curve: TCurve;
    dcurve: TDCurve;
begin
    curve[10].x := 10;
    dcurve.b[20].y := 20;
    writeln(curve[10].x);
    writeln(dcurve.b[20].y);
end.