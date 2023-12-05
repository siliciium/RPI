import bluetooth
import select, errno
from socket import error as SocketError


from threading import Thread
import pyaudio, sys, os, time, traceback
import wave

# SYSTEM CONFIGURATION :
# ---------------------
# sudo nano /etc/systemd/system/dbus-org.bluez.service
# * ExecStart=/usr/libexec/bluetooth/bluetoothd --compat
# sudo chmod 777 /var/run/sdp
# sdptool add --channel=22 SP

global server_run
server_run = False
global socket_loop
socket_loop = True
global th_loop
th_loop = True
global th_read
th_read = True
global p
global stream
global isPlaying
global thread


global client_socket
client_socket = None
global allowed_addr
allowed_addr = "XX:XX:XX:XX:XX:XX"
global password
password = "XXXXXXXXXXXXXXXXXXXXXXXXX"

server_sock = bluetooth.BluetoothSocket( bluetooth.RFCOMM )

port = 22
server_sock.bind(("", port))
server_sock.listen(1)
sys.stdout.write("RFCOMM server running on port %d\n" % port)
sys.stdout.flush()

readable = [server_sock]



def play_audio(audio_file = '/tmp/f', CHUNK = 1024*4):
	global th_loop, th_read, isPlaying

	while th_loop:

		isPlaying = False

		try:

			p = pyaudio.PyAudio()

			sys.stdout.write("Thread is wait for fifo datas...\n")
			sys.stdout.flush()

			wf = wave.open(audio_file, 'rb')
			sys.stdout.write("Audio Play starts\n")
			sys.stdout.flush()

			stream = p.open(format=p.get_format_from_width(wf.getsampwidth()),
				         channels=wf.getnchannels(),
				         rate=wf.getframerate(),
				         output=True)

			isPlaying = True

			data = wf.readframes(CHUNK)

			while len(data)!=0 and th_read:
				stream.write(data)
				data = wf.readframes(CHUNK)

			stream.stop_stream()
			stream.close()

			p.terminate()
			sys.stdout.write("Audio play finishes\n")
			sys.stdout.flush()


		except wave.Error as e:
			sys.stdout.write("wave.Error\n")
			sys.stdout.flush()
			#pass

		except EOFError as e:
			sys.stdout.write("EOFError\n")
			sys.stdout.flush()
			#pass

		except BaseException as e:
			traceback.print_exc()			

		time.sleep(.001)



def audio_thread():
	global thread
	thread = Thread(target = play_audio, args = ())
	thread.start()


def server_handle():
	global socket_loop, server_run, th_read, isPlaying, client_socket, allowed_addr

	
	sys.stdout.write("server_handle\n")
	sys.stdout.flush()

	if not server_run:
		audio_thread()

	if client_socket is not None:
		s = client_socket.send(b'READY')
		sys.stdout.write("Sending READY (%d byte) to client...\n" % s)
		sys.stdout.flush()

	i = 0
	with open("/tmp/f","wb") as fifo:

		server_run = True

		while socket_loop:
		
			try:

				r,w,e = select.select(readable,[],[],0)
				for rs in r:
					if rs is server_sock:
						c,a = server_sock.accept()

						client_socket = c

						if str(a[0]) == allowed_addr:
							sys.stdout.write("RFCOMM %s connected\n" % str(a))
							sys.stdout.flush()
							readable.append(c)

							sys.stdout.write("Sending HELLO to client %s\n" % str(a[0]))
							sys.stdout.flush()

							s = client_socket.send(b'HELLO')

							sys.stdout.write("%d bytes sent ...\n" % s)
							sys.stdout.flush()

						else:
							s = client_socket.send(b'REFUSED')

							sys.stdout.write("Sending REFUSED to client %s\n" % str(a[0]))
							sys.stdout.flush()

							client_socket.close()
							client_socket = None

					else:
						# read from a client
						data = rs.recv(1024)
						if not data:
							sys.stdout.write("%s disconnected\n" % str(rs.getpeername()))
							sys.stdout.flush()
							readable.remove(rs)
							rs.close()
						else:
							#print('\r{}:'.format(rs.getpeername()),len(data), data)


							if data.startswith(b'oauth:'):
								if data != str("oauth:%s"%password).encode():
									sys.stdout.write("Receiving INVALID oauth password from %s\n" % str(rs.getpeername()))
									sys.stdout.flush()
									s = client_socket.send(b'403:BADAUTH')
									client_socket.close()
									client_socket = None
								else:
									sys.stdout.write("Receiving VALID oauth password from %s\n" % str(rs.getpeername()))
									sys.stdout.flush()
									s = client_socket.send(b'200:OK')

							if data.startswith(b'RIFF'):
								sys.stdout.write("NEW FILE TO PLAY, isPlaying: %d\n" % isPlaying)
								sys.stdout.flush()

							if b'--REINIT--' in data:
								sys.stdout.write("RFCOMM : %s\n" % data.decode())
								sys.stdout.flush()
								socket_loop = False
								break
							else:
								fifo.write(data)

				i += 1
				print('/-\|'[i%4]+'\r',end='',flush=True)
				
				if socket_loop:
					time.sleep(.001)



			except SocketError as e:

				if e.errno != errno.ECONNRESET:
					raise
				else:
					sys.stdout.write("ECONNRESET\n")
					sys.stdout.flush()
					if len(readable) == 2:
						del readable[-1]
					client_socket = None
				pass



		fifo.close()


	th_read = False
	while isPlaying:
		time.sleep(.2)
	th_read = True
	
	socket_loop = True
	server_handle()
		



if __name__ == '__main__':
	try:
		if not os.path.exists("/tmp/f"):
			os.mkfifo("/tmp/f")

		server_handle()
	except KeyboardInterrupt:
		if server_run:
			server_sock.close()
			socket_loop = False
			server_run = False
	except Exception as e:
		print(e)

	th_loop = False
	thread.join()

	sys.stdout.write("Bye !\n")
	sys.stdout.flush()

	if server_run:
		server_sock.close()
