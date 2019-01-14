type
    TPoint = record
        x, y: integer;
    end;

var
    a, b: TPoint;

begin
    a.x := 10;
    b := a;
    writeln(b.x);
end.