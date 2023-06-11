package main

import (
	"fmt"
	"net"
	"os"
	"reflect"
	// "io"
)

func checkError(err error) {
	if err != nil {
		fmt.Printf("Error: %s\n", err.Error())
		os.Exit(1)
	}
}

func listen(conn net.Conn) {
	var buf [1400]byte

	for {
		n, err := conn.Read(buf[:])
		if err != nil {
			fmt.Printf("Error: %s\n", err.Error())
			return
		}

		fmt.Println(" recv msg", string(buf[:n]), "end")
	}

}

func main() {
	conn, err := net.Dial("udp", "127.0.0.1:9999")
	checkError(err)
	fmt.Printf("%T\n", reflect.TypeOf(conn))
	defer conn.Close()

	go listen(conn)
	// go func() {
	for {
		var input_msg string
		fmt.Print("send: ")
		fmt.Scanln(&input_msg)
		_, err = conn.Write([]byte(input_msg))
		checkError(err)
	}
	// }()

}
