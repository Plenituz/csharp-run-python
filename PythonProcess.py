import types
import traceback
from ClassCreator import ClassCreator
import json


#https://gist.github.com/ax3l/59d92c6e1edefcef85ac2540eb056da3
def imports():
	for name, val in globals().items():
		# module imports
		if isinstance(val, types.ModuleType):
			yield name, val
		# functions / callables
		if hasattr(val, '__call__'):
			yield name, val
noglobal = lambda fn: types.FunctionType(fn.__code__, dict(imports()))

COMPILE_CODE = "COMPILE_CODE"			#must send COMPILE_CODE
END_COMPILE_CODE = "END_COMPILE_CODE"	#must send "END_COMPILE_CODE:key"
RUN_CODE = "RUN_CODE"					#must send "RUN_CODE:key" after compiling code at given key
UPDATE_PROXY_VALUE = "UPDATE_PROXY_VALUE:"

STOP = "STOP"
traceback_template = '''Traceback (most recent call last):
  File "%(filename)s", line %(lineno)s, in %(name)s
%(type)s: %(message)s\n'''

STATE_WAITING = 0
STATE_READING_COMPILE = 1
STATE_RUNNING = 2

#revive the process as soon as it dies in c#
keepGoing = True
proxy = None
state = STATE_WAITING
buff = ''

def print_traceback(ex, action):
	allTb = traceback.extract_tb(ex.__traceback__)
	print("STDERR:Error while {0} python script".format(action))
	for i in range(1, len(allTb)):
		print("STDERR:line {0} in {1} {2}".format(allTb[i][1]-4, allTb[i][2], allTb[i][3]))
	print("STDERR:reason: "+ str(ex))
	if action is 'compiling':
		print("STDERR:the line number may not be accurate")

def runProxy():
	try:
		proxy.user_method()
	except Exception as ex:
		print_traceback(ex, 'running')

def updateCSharpProperty(name, value):
	print("UPDATE_PROXY_VALUE:" + name + ":" + str(value))

def callCSharpMethod(name, args):
	print("CALL_METHOD:" + name + ":" + json.dumps(args))
	inp = input()
	decoded = json.loads(inp)
	if('answer' in decoded):
		return decoded['answer']
	else:
		raise ValueError(decoded['exception'])

while(keepGoing):
	inp = input()

	if(UPDATE_PROXY_VALUE in inp):
		split = inp.split(':', 2)
		name = split[1]
		value = split[2]
		exec('proxy._%s = %s' % (name, value))
		continue

	if(state == STATE_WAITING):
		#if we are waiting, check for different instructions
		if(STOP in inp):
			keepGoing = False
			continue

		if(COMPILE_CODE in inp):
			buff = ""
			proxy = None
			CSharpProxy = None
			state = STATE_READING_COMPILE
			continue

		if(RUN_CODE in inp):
			assert proxy is not None, "you need to compile the code before running it"
			state = STATE_RUNNING
			runProxy()
			state = STATE_WAITING
			print("END_RUN_CODE")
			continue

	if(state == STATE_READING_COMPILE):
		#if we are reading code to compile check for end of compile
		#or just append to buffer
		if(END_COMPILE_CODE in inp):

			creator = ClassCreator()
			decoded = json.loads(buff)
			creator.setUserCode(decoded['userCode'])
			creator.setProperties(decoded['properties'])
			creator.setMethods(decoded['functions'])

			try:
				exec(creator.create_class_string())
				CSharpProxy = eval('%s' % 'CSharpProxy')
				proxy = CSharpProxy()
			except Exception as ex:
				print_traceback(ex, 'compiling')

			state = STATE_WAITING
			buff = ""
			print("DONE_COMPILE_CODE")
			continue
		else:
			buff += inp + "\n"
			continue

