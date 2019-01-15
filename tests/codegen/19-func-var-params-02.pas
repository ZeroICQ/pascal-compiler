type point = record x,y:integer; end;

procedure change(var i : integer; var c : double; var r : point);
begin
    i := 123;
    c := 12.12;
    r.x := 78;
    r.y := 90;
end;


var c : double;
var i : integer;
var r : point;

begin
    change(i, c, r);
    writeln(i, ' ', c);
    writeln(r.x, ' ', r.y);
end.