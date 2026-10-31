using System.Collections.ObjectModel;

namespace TU_Template.Models;

public static class GroupExtensions
{
    // Преобразует Group в DeviceGroup (для UI)
    public static DeviceGroup ToDeviceGroup(this Group group)
    {
        var deviceGroup = new DeviceGroup(group.Name);

        return deviceGroup;
    }

    // Обратное преобразование: DeviceGroup → Group
    public static Group ToGroup(this DeviceGroup deviceGroup)
    {
        var group = new Group(deviceGroup.Name);
        
        // Сохраняем все типы как список строк
        group.DeviceTypes = deviceGroup.Types.Select(t => t.Name).ToList();
        
        return group;
    }
}