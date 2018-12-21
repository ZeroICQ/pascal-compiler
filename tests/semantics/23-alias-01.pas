type
    myint = Integer;
    yourint = myint;
    
    myfloat = Float;
    yourfloat = myfloat;
    theirsfloat = yourfloat;

var
    a : MyInt;
    b : yourint;
    c : Integer;
    
    f1 : float; f2 : myfloat; f3 : yourfloat; f4 : theirsfloat;
 
begin
    c := a + b;
    
    f4 := f3 / f2 * f1 + a;
end.