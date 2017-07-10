import json

class ClassCreator(object):
	propertySetterTemplate = '''
	@property
	def {0}(self):
		return self._{0}

	@{0}.setter
	def {0}(self, value):
		updateCSharpProperty('{0}', value)
		self._{0} = value

	@{0}.deleter
	def {0}(self):
		del self._{0}'''

	propertyInitTemplate = '''
	\tself._{0} = None'''

	methodTemplate = '''
		def {0}({1}):
			return callCSharpMethod('{0}', [{1}])'''

	classTemplate = '''
class CSharpProxy(object):
	@noglobal
	def user_method(self):
	{3}
{1}
	

	def __init__(self):
		{0}

	{2}

	
	'''

	def __init__(self):
		self.property_init = ""
		self.property_setters = ""
		self.methods = ""

	def setUserCode(self, userCode):
		self.user_code = ''.join(["\t\t{0}\n".format(line) for line in userCode.splitlines()])

	def setProperties(self, propNameList):
		self.property_init = ''.join([ClassCreator.propertyInitTemplate.format(name) for name in propNameList])
		self.property_setters = ''.join([ClassCreator.propertySetterTemplate.format(name) for name in propNameList])
		
		#print(self.property_init)
		#print(self.property_setters)

	def setMethods(self, dictList):
		self.methods = ''.join([ClassCreator.methodTemplate.format(entry['name'], 
			''.join(['{0},'.format(arg) for arg in entry['args']])[:-1]
			#"" if len(entry['args']) == 0 else ","
			) for entry in dictList])
		#print(self.methods)

	def create_class_string(self):
		s = ClassCreator.classTemplate.format(self.property_init, self.user_code, self.property_setters, self.methods)
		#print(s)
		return s




'''
classStr = '{"properties":["StringProperty","IntProperty","FloatProperty"],"functions":[{"hasReturn":false,"name":"SimpleMethod","args":[]},{"hasReturn":false,"name":"MethodWithArg","args":["arg", "pd"]},{"hasReturn":true,"name":"MethodWithReturn","args":[]},{"hasReturn":true,"name":"MethodWithReturnAndArg","args":["arg"]}]}'
decoded = json.loads(classStr)
creator = ClassCreator()
creator.setUserCode("print('l')")
creator.setProperties(decoded['properties'])
creator.setMethods(decoded['functions'])
classStr = creator.create_class_string()
file = open("outputclass.py", "w")
file.write(classStr)
file.close()
'''