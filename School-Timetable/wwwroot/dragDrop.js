window.dragDrop = {
    init: function(objRef) {
        interact('.timetable-event').draggable({
            inertia: false,

            modifiers: [
                interact.modifiers.restrict({
                    restriction: '.calendar-grid',
                    endOnly: true,
                    elementRect: { top: 0, left: 0, bottom: 1, right: 1 }
                })
            ],

            autoScroll: true,

            listeners: {
                start(event) {
                    const target = event.target;
                    const originalSlot = target.closest('.time-slot');
                    if (originalSlot)
                        target.setAttribute('data-original-slot-id', originalSlot.getAttribute('data-slot-id'));
                },

                move(event) {
                    const target = event.target;
                    const x = (parseFloat(target.getAttribute('data-x')) || 0) + event.dx;
                    const y = (parseFloat(target.getAttribute('data-y')) || 0) + event.dy;

                    target.style.transform = 'translate(' + x + 'px, ' + y + 'px)';
                    target.setAttribute('data-x', x);
                    target.setAttribute('data-y', y);
                },

                end(event) {
                    const target = event.target;
                    const closestSlot = findClosestSlot(target);

                    if (!closestSlot)
                    {
                        resetPosition(target);
                        return;
                    }
                        
                    const eventId = target.getAttribute('data-event-id');
                    const targetSlotId = closestSlot.getAttribute('data-slot-id');
                    const originalSlotId = target.getAttribute('data-original-slot-id');
                    
                    if (targetSlotId !== originalSlotId)
                        objRef.invokeMethodAsync('MoveEvent', eventId, targetSlotId).catch(error => {
                            console.error("Error moving event: ", error);
                        });
                    else
                        resetPosition(target);
                }
            }
        });

        function resetPosition(element) {
            element.style.transform = '';
            element.setAttribute('data-x', 0);
            element.setAttribute('data-y', 0);
        }

        function findClosestSlot(draggedElement) {
            const slots = document.querySelectorAll('.time-slot');
            const draggedRect = draggedElement.getBoundingClientRect();

            // Calculate the center of the dragged element
            const draggedCenterX = draggedRect.left + draggedRect.width / 2;
            const draggedCenterY = draggedRect.top + draggedRect.height / 2;

            let closestSlot = null;
            let closestDistance = Infinity;

            slots.forEach(slot => {
                const slotRect = slot.getBoundingClientRect();

                // Calculate the center of each slot element
                const slotCenterX = slotRect.left + slotRect.width / 2;
                const slotCenterY = slotRect.top + slotRect.height / 2;

                // Calculate the distance between the centers
                const distance = Math.sqrt(
                    Math.pow(draggedCenterX - slotCenterX, 2) +
                    Math.pow(draggedCenterY - slotCenterY, 2)
                );

                // If this distance is the smallest so far, set this slot as the closest
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestSlot = slot;
                }
            });

            return closestSlot;
        }
    }
};