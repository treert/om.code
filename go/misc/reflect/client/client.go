package main

import (
	"fmt"
	"net"
	"os"
	// "io"
)

func checkError(err error) {
	if err != nil {
		fmt.Printf("Error: %s\n", err.Error())
		os.Exit(1)
	}
}

const port = 9999

func server() {
	udp_addr, err := net.ResolveUDPAddr("udp", ":9999")
	checkError(err)
	conn, err := net.ListenUDP("udp", udp_addr)
	checkError(err)
	defer conn.Close()
	var buf [1400]byte
	for {
		n, raddr, err := conn.ReadFromUDP(buf[0:])
		checkError(err)
		fmt.Printf("recv msg %s from %v\n", string(buf[:n]), raddr)
	}
}

func client() {
	conn, err := net.Dial("udp", "127.0.0.1:9999")
	checkError(err)
	// fmt.Printf("%T\n", reflect.TypeOf(conn))
	defer conn.Close()
	for {
		var input_msg string
		fmt.Print("send: ")
		fmt.Scanln(&input_msg)
		_, err = conn.Write([]byte(input_msg))
		checkError(err)
	}
}

func main() {

	go server()

	client()

}
