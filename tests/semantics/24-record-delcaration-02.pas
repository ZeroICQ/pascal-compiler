type
    Point = Record
        X,Y,Z : double;
        s : char;
        meh : integer;
    end;
    
    Image = Record
        x, y : double;
        wall: char = 's';
        price: integer = 2 + 2 * 200;
    end;
    
type
    Gallery = record
        points: array[2..20] of Array[1..100] of point;
        myimage : Image;
    end;

var 
    mypoint : point;
    myimage: image;
    mygallery : gallery;
    
    anotherpoint : point;
begin

    anotherpoint := mypoint;
    //myimage := mypoint;
end.
  