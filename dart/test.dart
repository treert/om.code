
import 'dart:mirrors';
void test_switch(pair) {
  switch (pair) {
    case (int a, int b):
      if (a > b) print('First element greater');
    // If false, prints nothing and exits the switch.
    // case (int a, int b) when a > b:
    //   // If false, prints nothing but proceeds to next case.
    //   print('First element greater');
    case (int, int):
      print('First element not greater');
  }

  print('test_switch');
}

void printAllType(Type t){
  var cm = reflectClass(t);
  for(;;){
    print('class: ${cm.qualifiedName}');
    if(cm.superclass != null){
      cm = cm.superclass!;
    }
    else{
      break;
    }
  }
  print('');
}

mixin class Base{
  void hello() => print('hello ${this.runtimeType}');
}

class T1 extends Base{

}

mixin class T2 implements Base {
  @override
  void hello() {
    print('hello T2!');
  }
}

class T3 with Base{

}

class T4 with T2{

}

class T5 extends T2{

}

void main() {
  {
    printAllType(T1);
    printAllType(T2);
    printAllType(T3);
    printAllType(T4);
    printAllType(T5);
  }
  {
    test_switch(null);
  }
  {
    const a = 'a';
    const b = 'b';
    var obj = ["a","b"];
    switch (obj) {
      // List pattern [a, b] matches obj first if obj is a list with two fields,
      // then if its fields match the constant subpatterns 'a' and 'b'.
      case [a, b]:
        print('$a, $b');
    }
  }

  {
    ({int a, int b}) recordA = (a: 1, b: 2);
    ({int b, int a}) recordB = (b: 2, a: 1);

    print((recordA, recordB, recordA == recordB));
  }
  // {
  //   for (var i = 0; i < 10; i++) {
  //     var kk = 1_000_000;
  //     var k = int.parse("1000000");
  //     print('hello ${i + 1} k=${k+kk}');
  //   }
  // }
}
