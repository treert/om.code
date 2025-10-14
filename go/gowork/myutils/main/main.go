package main

import (
	"fmt"
	"unsafe"
)

func main() {
	fmt.Println("go run myutils/main")
	var s = "123456"
	// var bb = []byte(s)
	var bb = unsafe.Slice(unsafe.StringData(s), len(s))
	fmt.Printf("%p %p %p %p\n", unsafe.SliceData(bb), unsafe.StringData(s), &bb, &s)
	bb[0] = 'a'
	fmt.Printf("%p %p %p %p\n", unsafe.SliceData(bb), unsafe.StringData(s), &bb, &s)
	fmt.Println(string(bb), s)
}
